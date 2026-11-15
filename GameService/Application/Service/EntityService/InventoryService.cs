using Application.Interface.Cache;
using Application.Interface.Realtime.Events;
using Application.Interface.Realtime.Events.Game;
using Contract;
using Contract.Enum.MetaDomain.Item;
using Domain.DomainException;
using Domain.Runtime.EntityDomain;
using Domain.Runtime.EntityDomain.Component;
using Domain.Runtime.MetaDomain;
using ResponseCode;

namespace Application.Service.EntityService
{
    public class InventoryService
    {
        #region Attributes
        private readonly ICacheProvider cacheProvider;
        private readonly IEventBus eventBus;
        #endregion

        public InventoryService(
            ICacheProvider cacheProvider,
            IEventBus eventBus)
        {
            this.cacheProvider = cacheProvider;
            this.eventBus = eventBus;
        }

        #region Methods
        /// <summary>
        /// Removes all items from the entity's inventory and returns them as a collection
        /// for the caller to handle (e.g., spawning world item entities).
        /// </summary>
        /// <param name="entity">The entity whose inventory will be emptied.</param>
        /// <returns>
        /// A list containing every item that was removed from the inventory.
        /// </returns>
        public ItemInstance DropItem(
            EntityInstance entity,
            string itemInstanceId)
        {
            var inventory = entity.GetComponent<InventoryInstance>();
            if (inventory == null)
                throw new InternalException(
                    ApplicationCode.InventoryServiceCode.DropSourceInventoryMissing,
                    $"Source entity {entity.ID} does not possess an InventoryInstance component.");

            var item = inventory.Items.FirstOrDefault(i => i.ID == itemInstanceId);
            if (item == null)
                throw new NotFound(
                    ApplicationCode.InventoryServiceCode.DropItemNotFound,
                    $"Entity {entity.ID} has no item {itemInstanceId}.");

            if (item.IsEquipped())
                throw new BadRequest(
                    ApplicationCode.InventoryServiceCode.DropItemEquipped,
                    $"Item {itemInstanceId} is equipped and can't be dropped.");

            return RemoveItem(entity, item);
        }

        public List<ItemInstance> DropAllItems(
            EntityInstance entity)
        {
            var sourceInventory = entity.GetComponent<InventoryInstance>();
            if (sourceInventory == null)
                throw new InternalException(
                    ApplicationCode.InventoryServiceCode.DropSourceInventoryMissing,
                    $"Source entity {entity.ID} does not possess an InventoryInstance component.");

            var drops = sourceInventory.Items.ToList();
            sourceInventory.Items.Clear();

            eventBus.Publish(new InventoryClearedEvent(entity.ID));

            return drops;
        }

        /// <summary>
        /// Removes up to <paramref name="count"/> random unequipped item stacks from the entity's inventory
        /// and returns them for the caller to spawn as world items.
        /// </summary>
        public List<ItemInstance> DropRandomItems(
            EntityInstance entity,
            int count)
        {
            var sourceInventory = entity.GetComponent<InventoryInstance>();
            if (sourceInventory == null)
                throw new InternalException(
                    ApplicationCode.InventoryServiceCode.DropSourceInventoryMissing,
                    $"Source entity {entity.ID} does not possess an InventoryInstance component.");

            var drops = sourceInventory.Items
                .Where(i => !i.IsEquipped())
                .OrderBy(_ => Random.Shared.Next())
                .Take(count)
                .ToList();

            foreach (var item in drops)
            {
                sourceInventory.Items.Remove(item);
                eventBus.Publish(new InventoryItemChangedEvent(entity.ID, item, ItemInventorySyncEvent.Removed));
            }

            return drops;
        }

        /// <summary>
        /// Attempts to transfer a world item payload into an entity's inventory,
        /// respecting stack limits and inventory slot capacity.
        /// The payload instance is updated in-place to reflect any remaining amount.
        /// A remaining amount of zero indicates the world item was fully collected.
        /// </summary>
        public bool TryPickItem(
            EntityInstance entity,
            EntityInstance worldItem)
        {
            var payload = worldItem.GetComponent<WorldItemPayloadInstance>();
            if (payload == null)
                return false;

            var inventory = entity.GetComponent<InventoryInstance>();
            if (inventory == null)
                return false;

            var inventoryDef = cacheProvider.Inventory.Get(inventory.DefinitionID);
            if (inventoryDef == null)
                return false;

            var item = payload.Payload;

            var itemDef = cacheProvider.Item.Get(item.DefinitionID);
            if (itemDef == null)
                return false;

            int remaining = item.Amount;
            int maxStack = itemDef.MaxStack ?? 1;

            //─────────────────────────────
            // 1. Fill existing stacks
            //─────────────────────────────
            if (maxStack > 1)
            {
                foreach (var slot in inventory.Items.Where(x =>
                    x.DefinitionID == item.DefinitionID &&
                    x.Quality == item.Quality))
                {
                    int canTake = maxStack - slot.Amount;

                    if (canTake <= 0)
                        continue;

                    int toAdd = Math.Min(canTake, remaining);

                    slot.AddAmount(toAdd);

                    eventBus.Publish(new InventoryItemChangedEvent(entity.ID, slot, ItemInventorySyncEvent.Updated));

                    remaining -= toAdd;

                    if (remaining == 0)
                        break;
                }
            }

            //─────────────────────────────
            // 2. Create new stacks
            //─────────────────────────────
            while (remaining > 0 && inventory.Items.Count < inventoryDef.SlotCount)
            {
                int toCreate = Math.Min(maxStack, remaining);

                var newSlot = new ItemInstance(
                    id: Guid.NewGuid().ToString(),
                    definitionId: item.DefinitionID,
                    amount: toCreate,
                    quality: item.Quality,
                    durability: item.Durability);

                inventory.Items.Add(newSlot);

                eventBus.Publish(new InventoryItemChangedEvent(entity.ID, newSlot, ItemInventorySyncEvent.Added));

                remaining -= toCreate;
            }

            // Update the payload remaining in the world.
            payload.Payload.SetAmount(remaining);

            return payload.Payload.Amount <= 0;
        }

        /// <summary>
        /// Equips an item on an entity and notifies the frontend of updated states
        /// for both the newly equipped item and any swapped-out item.
        /// </summary>
        public ItemInstance? EquipItem(
            EntityInstance entity,
            ItemInstance item,
            EquipmentSlot slot)
        {
            var inventory = entity.GetComponent<InventoryInstance>();
            if (inventory == null)
                return null;

            var unequippedItem = inventory.Equip(item, slot);

            // Notify UI for the newly equipped item
            eventBus.Publish(new InventoryItemChangedEvent(entity.ID, item, ItemInventorySyncEvent.Updated));

            // Notify UI for the unequipped/swapped item if one existed
            if (unequippedItem != null)
            {
                eventBus.Publish(new InventoryItemChangedEvent(entity.ID, unequippedItem, ItemInventorySyncEvent.Updated));
            }

            return unequippedItem;
        }

        /// <summary>
        /// Unequips an item from the given slot on an entity and notifies the frontend.
        /// </summary>
        public ItemInstance? UnequipItem(
            EntityInstance entity,
            EquipmentSlot slot)
        {
            var inventory = entity.GetComponent<InventoryInstance>();
            if (inventory == null)
                return null;

            var unequippedItem = inventory.Unequip(slot);
            if (unequippedItem != null)
            {
                eventBus.Publish(new InventoryItemChangedEvent(entity.ID, unequippedItem, ItemInventorySyncEvent.Updated));
            }

            return unequippedItem;
        }

        /// <summary>
        /// Removes an exact reference of an item stack entirely from an entity's inventory.
        /// </summary>
        public ItemInstance RemoveItem(
            EntityInstance entity,
            ItemInstance item)
        {
            var inventory = entity.GetComponent<InventoryInstance>();
            if (inventory == null)
                throw new InternalException(
                    ApplicationCode.InventoryServiceCode.RemoveTargetInventoryMissing,
                    $"Target entity {entity.ID} does not possess an InventoryInstance component.");

            if (inventory.Items.Remove(item))
                eventBus.Publish(new InventoryItemChangedEvent(entity.ID, item, ItemInventorySyncEvent.Removed));

            return item;
        }

        /// <summary>
        /// Deducts a single quantity unit from an item stack.
        /// If the stack becomes empty, it is automatically removed from the inventory.
        /// </summary>
        public void DeductItem(
            EntityInstance entity,
            ItemInstance item)
        {
            item.RemoveAmount(Constraint.ITEM_DEDUCTED_VALUE);
            if (entity.GetComponent<InventoryInstance>() != null && item.Amount <= 0)
            {
                RemoveItem(entity, item);
            }
            else
            {
                eventBus.Publish(new InventoryItemChangedEvent(entity.ID, item, ItemInventorySyncEvent.Updated));
            }
        }

        /// <summary>
        /// Lowers the durability tracking values of a target item, removing it entirely if it breaks.
        /// </summary>
        public void DegradeItem(
            EntityInstance entity,
            ItemInstance item)
        {
            if (entity.GetComponent<InventoryInstance>() != null && item.DegradeDurability())
            {
                RemoveItem(entity, item);
            }
            else
            {
                eventBus.Publish(new InventoryItemChangedEvent(entity.ID, item, ItemInventorySyncEvent.Updated));
            }
        }
        #endregion
    }
}