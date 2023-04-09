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
namespace Beta_Site_Elevator_V1
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
        #region Beta_Site_Elevator_V1

        //GLOBALS
        Dictionary<string, string> Blocks = new Dictionary<string, string>() {
            {"Controller", "Beta Site - PB - Main Elevator Controller"},
            {"ShaftPistons", "Beta Site - Piston - Main Elevator - Shaft"},
            {"GantryPistons", "Beta Site - Piston - Main Elevator - Gantry"},
            {"Locks", "Beta Site - Connector - Main Elevator - Lock"},
            {"DoorHinges", "Beta Site - Hinge - Main Elevator - Door"},
            {"WarningLights", "Beta Site - Light - Main Elevator - Warning"}
        };

        Dictionary<string, string> Floors = new Dictionary<string, string>();

        //DATA
        float ElevatorSpeed;
        float GantrySpeed;
        float DoorSpeed;
        string Status;
        string TargetFloor;
        bool GantryRetracted;

        List<IMyTerminalBlock> ShaftPistons = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> GantryPistons = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> Locks = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> DoorHinges = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> WarningLights = new List<IMyTerminalBlock>();

        IMyCubeGrid grid;
        IMyTerminalBlock Controller;


        //METHODS
        public void GetBlocks()
        {
            grid = Me.CubeGrid as IMyCubeGrid;
            Controller = GridTerminalSystem.GetBlockWithName(Blocks["Controller"]);
            GridTerminalSystem.SearchBlocksOfName(Blocks["ShaftPistons"], ShaftPistons);
            GridTerminalSystem.SearchBlocksOfName(Blocks["GantryPistons"], GantryPistons);
            GridTerminalSystem.SearchBlocksOfName(Blocks["Locks"], Locks);
            GridTerminalSystem.SearchBlocksOfName(Blocks["DoorHinges"], DoorHinges);
            GridTerminalSystem.SearchBlocksOfName(Blocks["WarningLights"], WarningLights);
        }

        public void GetCustomData()
        {
            string[] CustomData = Controller.CustomData.Split('\n');

            if (CustomData[0] == "")
            {
                SetCustomData();
            }
            else
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                foreach (string line in CustomData)
                {
                    string[] part = line.Split(':');

                    if (part[0].Contains('#'))
                    {
                        data.Add(part[0].Trim().Replace(' ', '_'), "");
                    }
                    else if (part[0].Contains('*'))
                    {
                        Floors[part[0].Replace('*', ' ').Trim().Replace(' ', '_')] = part[1].Trim();
                    }
                    else
                    {
                        data.Add(part[0].Trim().Replace(' ', '_'), part[1].Trim());
                    }
                }

                ElevatorSpeed = float.Parse(data["Elevator_Speed"]);
                GantrySpeed = float.Parse(data["Gantry_Speed"]);
                DoorSpeed = float.Parse(data["Door_Speed"]);
                Status = data["Status"];
                TargetFloor = data["Target_Floor"];
            }
        }

        public void SetCustomData()
        {
            string[] CustomData = Controller.CustomData.Split('\n');
            if (CustomData[0] == "")
            {
                Controller.CustomData = (
                    "#####Settings#####\n" +
                    "Elevator Speed: 3\n" +
                    "Gantry Speed: 1\n" +
                    "Door Speed: 0.5\n" +
                    "#####DATA - DON'T TOUCH####\n" +
                    "Status: idle\n" +
                    "Target Floor: none\n" +
                    "#####SAVED FLOORS#####"
                );
            }
            else
            {
                string FloorList = "";
                foreach (var pair in Floors)
                {
                    FloorList += "\n* ";
                    FloorList += pair.Key;
                    FloorList += ": ";
                    FloorList += pair.Value;
                }

                Controller.CustomData = (
                    "#####SETTINGS#####\n" +
                    "Elevator Speed: " + ElevatorSpeed + "\n" +
                    "Gantry Speed: " + GantrySpeed + "\n" +
                    "Door Speed: " + DoorSpeed + "\n" +
                    "#####DATA - DON'T TOUCH####\n" +
                    "Status: " + Status + "\n" +
                    "Target Floor: " + TargetFloor + "\n" +
                    "#####SAVED FLOORS#####" +
                    FloorList
                );
            }
        }

        public void CheckGantry()
        {
            GantryRetracted = true;
            foreach (IMyTerminalBlock Piston in GantryPistons)
            {
                var PistonTemp = Piston as IMyPistonBase;

                if (PistonTemp.Status.ToString() != "Retracted")
                {
                    GantryRetracted = false;
                }
            }
        }

        public void GantryControl()
        {
            if (Status == "idle")
            {
                foreach (IMyTerminalBlock Piston in GantryPistons)
                {
                    Piston.SetValue<float>("Velocity", GantrySpeed);
                }
            }
            else
            {
                foreach (IMyTerminalBlock Piston in GantryPistons)
                {
                    Piston.SetValue<float>("Velocity", -GantrySpeed);
                }
            }
        }

        public float GetCurrentHeight()
        {
            float total = 0;
            foreach (IMyTerminalBlock Piston in ShaftPistons)
            {
                var PistonTemp = Piston as IMyPistonBase;
                total += PistonTemp.CurrentPosition;
            }
            return total / 4;
        }

        public float GetSpeed(float CurrentHeight, float TargetHeight)
        {
            float Difference = Math.Abs(CurrentHeight - TargetHeight);
            float TargetSpeed = (ElevatorSpeed / 5) * Difference;

            if (Difference > 10)
            {
                return ElevatorSpeed / (ShaftPistons.Count() / 4);
            }
            else if (Difference < 0.025)
            {
                if (Status == "opening" || Status == "open" || Status == "closing")
                {
                    return 0;
                }
                else
                {
                    Status = "idle";
                }
                return 0;
            }
            else
            {
                return TargetSpeed / (ShaftPistons.Count() / 4);
            }
        }

        public void MoveElevator()
        {
            if (GantryRetracted)
            {
                float CurrentHeight = GetCurrentHeight();
                float TargetHeight = float.Parse(Floors[TargetFloor]);
                float TargetSpeed = GetSpeed(CurrentHeight, TargetHeight);

                if (TargetHeight < CurrentHeight)
                {
                    foreach (IMyTerminalBlock Piston in ShaftPistons)
                    {
                        Piston.SetValue<float>("Velocity", -TargetSpeed);
                    }
                }
                else if (TargetHeight > CurrentHeight)
                {
                    foreach (IMyTerminalBlock Piston in ShaftPistons)
                    {
                        Piston.SetValue<float>("Velocity", TargetSpeed);
                    }
                }
            }
        }

        public void ToggleLights(bool Activate)
        {
            foreach (IMyTerminalBlock Light in WarningLights)
            {
                if (Activate)
                {
                    Light.ApplyAction("OnOff_On");
                }
                else
                {
                    Light.ApplyAction("OnOff_Off");
                }
            }
        }

        public void ArgsInput(string arg)
        {
            string[] args = arg.Split(' ');

            if (args.Length == 1)
            {
                if (args[0].ToLower() == "manual")
                {
                    Status = "manual";
                }

                foreach (var floor in Floors)
                {
                    if (floor.Key == args[0] && Status == "idle")
                    {
                        TargetFloor = floor.Key;
                        Status = "moving";
                    }
                }
            }
            else if (args.Length == 2)
            {
                if (args[0].ToLower() == "remove")
                {
                    Floors.Remove(args[1]);
                }

                if (args[0].ToLower() == "add")
                {
                    Floors[args[1]] = GetCurrentHeight().ToString();
                }

                if (args[0].ToLower() == "launchbay")
                {
                    if (args[1].ToLower() == "open")
                    {
                        Status = "opening";
                    }

                    if (args[1].ToLower() == "close")
                    {
                        Status = "closing";
                    }
                }
            }
        }


        //UPDATE FREQUENCY
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        //MAIN
        public void Main(string args)
        {
            GetBlocks();
            GetCustomData();
            CheckGantry();
            GantryControl();
            ArgsInput(args);

            if (Status == "idle")
            {
                Echo("System Idle...");
                ToggleLights(false);
            }
            else if (Status == "moving")
            {
                MoveElevator();
            }
            else if (Status == "opening")
            {
                TargetFloor = "Top";
                MoveElevator();
                if (GetSpeed(GetCurrentHeight(), float.Parse(Floors[TargetFloor])) == 0)
                {
                    foreach (IMyTerminalBlock Lock in Locks)
                    {
                        Lock.ApplyAction("Unlock");
                    }

                    foreach (IMyTerminalBlock Door in DoorHinges)
                    {
                        Door.SetValue<float>("Velocity", DoorSpeed);
                    }

                    Status = "open";
                }
            }
            else if (Status == "closing")
            {
                bool DoorClosed = true;

                foreach (IMyTerminalBlock Door in DoorHinges)
                {
                    Door.SetValue<float>("Velocity", -DoorSpeed);

                    IMyMotorAdvancedStator DoorTemp = Door as IMyMotorAdvancedStator;

                    var DoorAngle = Math.Round(DoorTemp.Angle * (180.0f / Math.PI), 3);
                    float RestAngle = DoorTemp.GetValue<float>("LowerLimit");

                    if (DoorAngle != RestAngle)
                    {
                        DoorClosed = false;
                    }
                }

                if (DoorClosed)
                {
                    bool PistonsExtended = true;
                    foreach (IMyTerminalBlock Piston in ShaftPistons)
                    {
                        Piston.SetValue<float>("Velocity", 0.1f);
                        IMyPistonBase TempPiston = Piston as IMyPistonBase;
                        if (TempPiston.Status.ToString() != "Extended")
                        {
                            PistonsExtended = false;
                        }
                    }

                    if (PistonsExtended)
                    {
                        foreach (IMyTerminalBlock Lock in Locks)
                        {
                            Lock.ApplyAction("Lock");
                        }

                        TargetFloor = "Surface";
                        Status = "moving";
                        MoveElevator();
                    }
                }
            }

            if (Status != "idle")
            {
                ToggleLights(true);
            }

            SetCustomData();
        }

        #endregion // Beta_Site_Elevator_V1
    }
}