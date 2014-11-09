using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using Zeta.Bot;
using Zeta.Bot.Settings;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals.Actors;

namespace QuestTools.Helpers
{
    public class ItemManager
    {
        private static int _lastBackPackCount;
        private static int _lastProtectedSlotsCount;
        private static Vector2 _lastBackPackLocation = new Vector2(-2, -2);

        internal static void ResetBackPackCheck()
        {
            _lastBackPackCount = -1;
            _lastProtectedSlotsCount = -1;
            _lastBackPackLocation = new Vector2(-2, -2);
        }

        public static bool IsValidTwoSlotLocation()
        {
            return FindValidBackpackLocation(true) == new Vector2(-1, -1);
        }

        /// <summary>
        /// Search backpack to see if we have room for a 2-slot item anywhere
        /// </summary>
        /// <param name="isOriginalTwoSlot"></param>
        /// <returns></returns>
        internal static Vector2 FindValidBackpackLocation(bool isOriginalTwoSlot)
        {
            try
            {
                if (_lastBackPackLocation != new Vector2(-2, -2) &&
                    _lastBackPackCount == ZetaDia.Me.Inventory.Backpack.Count(i => i.IsValid) &&
                    _lastProtectedSlotsCount == CharacterSettings.Instance.ProtectedBagSlots.Count)
                {
                    return _lastBackPackLocation;
                }

                bool[,] backpackSlotBlocked = new bool[10, 6];

                int freeBagSlots = 60;

                _lastProtectedSlotsCount = CharacterSettings.Instance.ProtectedBagSlots.Count;
                _lastBackPackCount = ZetaDia.Me.Inventory.Backpack.Count(i => i.IsValid);

                // Block off the entire of any "protected bag slots"
                foreach (InventorySquare square in CharacterSettings.Instance.ProtectedBagSlots)
                {
                    backpackSlotBlocked[square.Column, square.Row] = true;
                    freeBagSlots--;
                }

                // Map out all the items already in the backpack
                foreach (ACDItem item in ZetaDia.Me.Inventory.Backpack)
                {
                    if (!item.IsValid)
                        continue;

                    int row = item.InventoryRow;
                    int col = item.InventoryColumn;

                    if (row < 0 || row > 5)
                    {
                        Logger.Error("Item {0} ({1}) is reporting invalid backpack row of {2}!",
                            item.Name, item.InternalName, item.InventoryRow);
                        continue;
                    }

                    if (row < 0 || row > 9)
                    {
                        Logger.Error("Item {0} ({1}) is reporting invalid backpack column of {2}!",
                            item.Name, item.InternalName, item.InventoryColumn);
                        continue;
                    }

                    // Slot is already protected, don't double count
                    if (!backpackSlotBlocked[col, row])
                    {
                        backpackSlotBlocked[col, row] = true;
                        freeBagSlots--;
                    }

                    if (!item.IsTwoSquareItem)
                        continue;

                    try
                    {
                        // Slot is already protected, don't double count
                        if (backpackSlotBlocked[col, row + 1])
                            continue;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        Logger.Error("Error checking for next slot on item {0}, row={1} col={2} IsTwoSquare={3} ItemType={4}",
                            item.Name, item.InventoryRow, item.InventoryColumn, item.ItemType);
                        continue;
                    }

                    freeBagSlots--;
                    backpackSlotBlocked[col, row + 1] = true;
                }

                bool noFreeSlots = freeBagSlots < 1;
                int unprotectedSlots = 60 - _lastProtectedSlotsCount;

                // free bag slots is less than required
                if (noFreeSlots || freeBagSlots < unprotectedSlots)
                {
                    Logger.Debug("Free Bag Slots is less than required. FreeSlots={0}, Protected={1} BackpackCount={2}",
                        freeBagSlots, _lastProtectedSlotsCount, _lastBackPackCount);

                    _lastBackPackLocation = new Vector2(-1, -1);
                    return _lastBackPackLocation;
                }

                // 10 columns
                for (int col = 0; col <= 9; col++)
                {
                    // 6 rows
                    for (int row = 0; row <= 5; row++)
                    {
                        // Slot is blocked, skip
                        if (backpackSlotBlocked[col, row])
                            continue;

                        // Not a two slotitem, slot not blocked, use it!
                        if (!isOriginalTwoSlot)
                        {
                            _lastBackPackLocation = new Vector2(col, row);
                            return _lastBackPackLocation;
                        }

                        // Is a Two Slot, Can't check for 2 slot items on last row
                        if (row == 5)
                            continue;

                        // Is a Two Slot, check row below
                        if (backpackSlotBlocked[col, row + 1])
                            continue;

                        _lastBackPackLocation = new Vector2(col, row);
                        return _lastBackPackLocation;
                    }
                }

                // no free slot
                Logger.Debug("No Free slots!");
                _lastBackPackLocation = new Vector2(-1, -1);
                return _lastBackPackLocation;
            }
            catch (Exception ex)
            {
                Logger.Log("Error in finding backpack slot");
                Logger.Debug("{0}", ex.ToString());
                return new Vector2(1, 1);
            }
        }
    }
}
