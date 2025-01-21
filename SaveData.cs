using System;
using System.Collections.Generic;

[Serializable] // Wish we could use dictionaries but they arent serializable by default in unity json so this is easier
public class GeneralDataKVPair {
    public string key;
    public string value;
}

// Data structure for items stored in the inventory
[Serializable]
public class Item
{
    public string itemID;
    public string itemName;
    public int quantity;

    public Item(string id, string name, int qty) { // Constructor for the item
        itemID = id;
        itemName = name;
        quantity = qty;
    }
}

[Serializable]
public class SerializedInventoryItem { // Wrapper for Item class since unity requires a parameterless constructor (unity c# be weird like that)
    public string itemID;
    public Item item;
}

[Serializable]
public class InventoryKVPair {
    public string key;
    public List<SerializedInventoryItem> inventory;

    public InventoryKVPair(string key) {
        this.key = key;
        this.inventory = new List<SerializedInventoryItem>();
    }
}

[Serializable]
public class SaveData
{
    // Queue System Area (The queue system is an internal queue system to ensure proper threadsafe loading and saving of data not meant for use by end users)
    // We are making to queue lists of type Action which we will use to tie callback functions to each update call creating a queueing system of saving
    // We have 2 as we want to have a double queue system that can alternate. So whenever SaveDataToSlot is called updateQueue1 will stop having new things added to it, and will run through all the queued updates. updateQueue2 will also be activated after 1 is deactivated to store the new incoming requests. Once the save is done queue2 will start processing its saving operations and then the process repeats
    private Queue<Action> updateQueue1 = new Queue<Action>();
    private Queue<Action> updateQueue2 = new Queue<Action>();
    private bool isSavingOrLoading = false;
    private bool queueToggle = false;

    public void FinishCurrQueueAndPause() { // DO NOT USE THIS FUNCTION
        // this function serves solely to trigger the activate queue function to ensure the current queue is empty before we save
        if (!isSavingOrLoading) {
            ActivateQueue();
        }
    }

    private void EnqueueUpdate(Action updateFunction) { // DO NOT TOUCH
        if (queueToggle) {
            // True will represent queue1
            updateQueue1.Enqueue(updateFunction);
        } else {
            // false will represent queue2
            updateQueue2.Enqueue(updateFunction);
        }

        if (!isSavingOrLoading) {
            ActivateQueue();
        }
    }

    private void ActivateQueue() { // DO NOT TOUCH
        isSavingOrLoading = true;
        Queue<Action> chosenQueue;
        if (queueToggle) {
            chosenQueue = updateQueue1;
        } else {
            chosenQueue = updateQueue2;
        }
        queueToggle = !queueToggle; //disables the currently active queue to allow for saving and enables the inactive queue to store save requests

        while (chosenQueue.Count > 0) {
            Action updateAction = chosenQueue.Dequeue();
            updateAction?.Invoke(); // ? checks if the updateAction is null or not also this calls hte update function
        }

        isSavingOrLoading = false;
    }



    // General data area
    public List<GeneralDataKVPair> generalData = new List<GeneralDataKVPair>();
    public void AddOrUpdate(string key, string value) {
        EnqueueUpdate(() => {
            GeneralDataKVPair pair = generalData.Find(x => x.key == key);
            if (pair != null) {
                pair.value = value;
            } else {
                generalData.Add(new GeneralDataKVPair { key = key, value = value });
            }
        });
    }

    public string GetValueFromKey(string key) {
        GeneralDataKVPair pair = generalData.Find(x => x.key == key);
        return pair != null ? pair.value : null;
    }

    public bool HasKey(string key) {
        return generalData.Exists(x => x.key == key);
    }



    // Inventory data area - using List of InventoryKVPair instead of Dictionary
    public List<InventoryKVPair> inventories = new List<InventoryKVPair>();

    public void CreateInventory(string inventoryName) {
        EnqueueUpdate(() => {
            // Check if the inventory already exists
            InventoryKVPair inventoryPair = inventories.Find(x => x.key == inventoryName);

            if (inventoryPair == null) {
                inventories.Add(new InventoryKVPair(inventoryName)); // Pass inventoryName to the constructor
            }
        });
    }

    public void AddOrUpdateInventoryItem(string inventoryName, string itemID, bool updateName, string itemName, bool updateQuantity, int quantity) {
        EnqueueUpdate(() => {
            InventoryKVPair inventoryPair = inventories.Find(x => x.key == inventoryName);

            if (inventoryPair == null) {
                return; // Do nothing if inventory doesn't exist
            }

            var inventory = inventoryPair.inventory;
            var existingItem = inventory.Find(x => x.itemID == itemID);

            if (existingItem != null) {
                if (updateQuantity) {
                    existingItem.item.quantity += quantity;
                }
                if (updateName) {
                    existingItem.item.itemName = itemName;
                }
            } else {
                inventory.Add(new SerializedInventoryItem {
                    itemID = itemID,
                    item = new Item(itemID, itemName, quantity)
                });
            }
        });
    }



    // Get all items in the inventory
    public List<Item> GetInventory(string inventoryName) {
        InventoryKVPair inventoryPair = inventories.Find(x => x.key == inventoryName);
        return inventoryPair != null ? inventoryPair.inventory.ConvertAll(x => x.item) : null; // lets us access 'item' directly
    }

    // Get a specific item by itemID
    public Item GetItemByID(string inventoryName, string itemID) {
        InventoryKVPair inventoryPair = inventories.Find(x => x.key == inventoryName);
        if (inventoryPair != null) {
            var item = inventoryPair.inventory.Find(x => x.itemID == itemID);
            return item != null ? item.item : null;  // Access 'item' directly
        }
        return null;
    }

    public void RemoveInventory(string inventoryName) {
        EnqueueUpdate(() => {
            InventoryKVPair inventoryPair = inventories.Find(x => x.key == inventoryName);
            if (inventoryPair != null) {
                inventories.Remove(inventoryPair);
            }
        });
    }

    public bool InventoryExists(string inventoryName) {
        return inventories.Exists(x => x.key == inventoryName);
    }
}