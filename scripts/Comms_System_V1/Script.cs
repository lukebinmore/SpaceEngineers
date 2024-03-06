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
using System.Collections.Immutable;
using Sandbox.Game.Entities.Cube;
using System.Reflection;
using System.Text.RegularExpressions;
using VRage.Utils;
using System.Linq;

/*
 * Must be unique per each script project.
 * Prevents collisions of multiple `class Program` declarations.
 * Will be used to detect the ingame script region, whose name is the same.
 */
namespace Comms_System_V1
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
        #region Comms_System_V1

        //GLOBALS
        public class Instruction
        {
            public string Name { get; set; }
            public string DeviceName { get; set; }
            public List<IMyTextPanel> Devices = new List<IMyTextPanel>();
            public string Text { get; set; }
            public bool Synced { get; set; }
            public bool Changed { get; set; }
            public bool Master { get; set; }

            public Instruction(string NameInput)
            {
                Name = NameInput;
                Text = "";
                Synced = false;
                Changed = false;
                Master = false;
            }

            public override string ToString()
            {
                string StringOutput = (
                    $"Name: {Name}\n" +
                    $"Devices: {DeviceName}\n"
                );

                return StringOutput;
            }
        }

        Dictionary<string, Instruction> Instructions = new Dictionary<string, Instruction>();
        List<IMyTerminalBlock> Antennas = new List<IMyTerminalBlock>();

        public Program() { Runtime.UpdateFrequency = UpdateFrequency.Update100; }

        public void PrintTerminal()
        {
            string[] lines = {
                "#####OPTIONS#####",
                "Name: Anything",
                "Master: True, False",
                "Devices: Block(s) Name",
                "#####EXAMPLE#####",
                "Name: ToDo List",
                "Master: True",
                "Devices: LCD - ToDo List",
                "(Empty Line To Seperate)"
            };
            bool FirstLine = true;

            foreach (string line in lines)
            {
                if (line.Contains('#') && !FirstLine) { Echo("\n" + line); }
                else { Echo(line); }
                FirstLine = false;
            }
        }

        public void GetBaseBlocks()
        {
            try
            {
                List<IMyTerminalBlock> LaserAntennas = new List<IMyTerminalBlock>();
                List<IMyTerminalBlock> RadioAntennas = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocksOfType<IMyLaserAntenna>(LaserAntennas);
                GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(RadioAntennas);

                if (RadioAntennas != null)
                {
                    Antennas = RadioAntennas;
                    if (LaserAntennas != null) { Antennas.AddRange(LaserAntennas); }
                }
                else if (LaserAntennas != null) { Antennas = LaserAntennas; }
                else { throw new Exception("Error - No Antennas Found!"); }
            }
            catch (Exception ex)
            {
                Echo(ex.Message);
            }

        }

        public void GetBlocks(string NameInput, string DeviceInput)
        {
            try
            {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.SearchBlocksOfName(DeviceInput, blocks);
                Instructions[NameInput].DeviceName = DeviceInput;

                foreach (IMyTerminalBlock block in blocks)
                {
                    if (block is IMyTextPanel)
                    {
                        Instructions[NameInput].Devices.Add(block as IMyTextPanel);
                    }
                }

                if (Instructions[NameInput].Devices.Count == 0)
                {
                    throw new Exception($"Unable to find device(s): {DeviceInput}");
                }
            }
            catch (Exception ex) { Echo($"Error - {ex.Message}"); }
        }

        public void GetCustomData()
        {
            try
            {
                string[] CustomData = Me.CustomData.Split(new string[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

                if (CustomData.Length == 0)
                {
                    Echo("\nNothing Configured!");
                }
                else
                {
                    foreach (string InstructionInput in CustomData)
                    {
                        Dictionary<string, string> data = new Dictionary<string, string>();
                        string[] lines = InstructionInput.ToLower().Split('\n');
                        data.Clear();

                        foreach (string line in lines)
                        {
                            if (line != "")
                            {
                                string[] part = line.Split(':');
                                data.Add(part[0].Trim(), part[1].Trim());
                            }
                        }
                        if (data["name"] == null) { throw new Exception("Name not provided!"); }
                        if (data["master"] == null) { throw new Exception("Master Status not provided!"); }
                        if (data["devices"] == null) { throw new Exception("Device not provided!"); }
                        if (!Instructions.ContainsKey(data["name"]))
                        {
                            Instructions[data["name"]] = new Instruction(data["name"]);
                        }
                        Instructions[data["name"]].Name = data["name"];
                        Instructions[data["name"]].Master = bool.Parse(data["master"]);
                        GetBlocks(data["name"], data["devices"]);
                    }
                }
            }
            catch (Exception ex)
            {
                Echo($"Error - {ex.Message}");
            }
        }

        public void DetectLocalChanges()
        {
            foreach (var Group in Instructions.Values)
            {
                if (Group.Master) { Group.Synced = true; }
                if (Group.Synced)
                {
                    foreach (IMyTextPanel LCD in Group.Devices)
                    {
                        if (LCD.GetText() != Group.Text)
                        {
                            if (LCD.GetText() != "")
                            {
                                Group.Text = LCD.GetText();
                                Group.Changed = true;
                                ApplyLocalChanges();
                                BroadcastMessage(Group.Name, "Update");
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void ApplyLocalChanges()
        {
            foreach (var Group in Instructions.Values)
            {
                foreach (IMyTextPanel LCD in Group.Devices)
                {
                    LCD.WriteText(Group.Text);
                }
            }
        }

        public void BroadcastMessage(string Name, string Message)
        {
            if (Message == "Pull")
            {
                IGC.SendBroadcastMessage("LCDPull", Name);
            }
            else if (Message == "Push")
            {
                IGC.SendBroadcastMessage("LCDPush", $"{Name}¬{Instructions[Name].Text}");
            }
            else if (Message == "Update")
            {
                IGC.SendBroadcastMessage("LCDUpdate", $"{Name}¬{Instructions[Name].Text}");
            }

        }

        public void ReceiveMessage()
        {
            IGC.RegisterBroadcastListener("LCDUpdate");
            IGC.RegisterBroadcastListener("LCDPull");
            IGC.RegisterBroadcastListener("LCDPush");

            List<IMyBroadcastListener> BroadcastListeners = new List<IMyBroadcastListener>();
            IGC.GetBroadcastListeners(BroadcastListeners);
            foreach (IMyBroadcastListener Listener in BroadcastListeners)
            {
                while (Listener.HasPendingMessage)
                {
                    string[] Message = Listener.AcceptMessage().Data.ToString().Split('¬');

                    if (Instructions.ContainsKey(Message[0]))
                    {
                        if (Listener.Tag == "LCDUpdate" && Instructions[Message[0]].Synced)
                        {
                            Instructions[Message[0]].Text = Message[1];
                            ApplyLocalChanges();
                        }
                        else if (Listener.Tag == "LCDPull")
                        {
                            BroadcastMessage(Message[0], "Push");
                        }
                        else if (Listener.Tag == "LCDPush")
                        {
                            Instructions[Message[0]].Text = Message[1];
                            ApplyLocalChanges();
                            Instructions[Message[0]].Synced = true;
                        }
                    }
                }
            }
        }

        public void InitialPull()
        {
            foreach (var Group in Instructions.Values)
            {
                if (!Group.Synced) { BroadcastMessage(Group.Name, "Pull"); }
            }
        }

        public void Main(string args)
        {
            GetBaseBlocks();
            PrintTerminal();
            GetCustomData();
            InitialPull();
            DetectLocalChanges();
            ReceiveMessage();
        }

        #endregion // Comms_System_V1
    }
}