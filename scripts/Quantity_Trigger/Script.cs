using System;

// Space Engineers DLLs
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using VRageMath;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;

/*
 * Must be unique per each script project.
 * Prevents collisions of multiple `class Program` declarations.
 * Will be used to detect the ingame script region, whose name is the same.
 */
namespace Quantity_Trigger
{

    /*
     * Do not change this declaration because this is the game requirement.
     */
    public sealed class Program : MyGridProgram
    {

        /*
         * Must be same as the namespace. Will be used for automatic script export.
         * The code inside this region is the ingame script.
         */
        #region Quantity_Trigger

        //GLOBALS
        Dictionary<string, double> items = new Dictionary<string, double>();

        //METHODS/FUNCTIONS
        public void GetItems()
        {
            items.Clear();

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyInventoryOwner>(blocks);
            foreach (var block in blocks)
            {
                if (block.HasInventory)
                {
                    var inventory = block.GetInventory(0);
                    if (block.InventoryCount > 1)
                    {
                        inventory = block.GetInventory(1);
                    }

                    List<MyInventoryItem> inventoryItems = new List<MyInventoryItem>();
                    inventory.GetItems(inventoryItems);

                    foreach (MyInventoryItem item in inventoryItems)
                    {
                        string itemType = item.Type.TypeId.ToString().Split('_')[1];
                        string itemName = itemType + "/" + item.Type.SubtypeId.ToString();
                        if (itemType == "Ore" || itemType == "Ingot" || itemType == "Component")
                        {
                            items[itemName] = items.ContainsKey(itemName) ? items[itemName] + (double)item.Amount : (double)item.Amount;
                        }
                    }
                }
            }
        }

        public void EchoOptions()
        {
            string intro = "### Examples ###\n*Ores/Stone < 40000\n*Ingots/Iron < 500\n=Drills\n\n";
            string ores = "### Possible Ores: ###\n";
            string ingots = "\n### Possible Ingots: ###\n";
            string components = "\n### Possible Components: ###\n";

            foreach (var item in items.OrderBy(x => x.Key))
            {
                string category = item.Key.Split('/')[0];
                switch (category)
                {
                    case "Ore":
                        ores += " - " + item.Key + "\n";
                        break;
                    case "Component":
                        components += " - " + item.Key + "\n";
                        break;
                    case "Ingot":
                        ingots += " - " + item.Key + "\n";
                        break;
                }
            }

            string output = intro + ores + components + ingots;

            Echo(output);
        }

        public void SetGroup(bool state, string group)
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(group.Split('=')[1], blocks);
            if (blocks.Count < 1)
            {
                GridTerminalSystem.GetBlockGroupWithName(group.Split('=')[1]).GetBlocksOfType(blocks);
            }
            foreach (var block in blocks)
            {
                block.ApplyAction(state ? "OnOff_On" : "OnOff_Off");
            }
        }

        public bool CheckCondition(string line)
        {
            string[] parts = line.Split(' ');
            if (items.ContainsKey(parts[0]))
            {

                switch (parts[1])
                {
                    case ">":
                        return items[parts[0]] > double.Parse(parts[2]) ? true : false;
                    case "<":
                        return items[parts[0]] < double.Parse(parts[2]) ? true : false;
                    default:
                        Echo(parts[2]);
                        Echo("###INVALID CHARACTER!!!###");
                        return false;
                }
            }
            return true;
        }

        public void CheckGroups()
        {
            string customData = Me.CustomData;
            string[] groups = customData.Split(new string[] { "\n\n" }, StringSplitOptions.None);

            foreach (string group in groups)
            {
                bool conditionsMet = true;
                string[] lines = group.Trim().Split('\n');
                foreach (string line in lines)
                {
                    if (line.Contains('*'))
                    {
                        conditionsMet = CheckCondition(line.Split('*')[1]) ? true : false;
                    }
                }

                if (lines.Last().Contains('='))
                {
                    SetGroup(conditionsMet, lines.Last());
                }
            }
        }

        //PROGRAM UPDATE FREQUENCY
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        //PROGRAM MAIN
        public void Main(string args)
        {
            GetItems();
            EchoOptions();
            CheckGroups();
        }

        #endregion // Quantity_Trigger
    }
}