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

/*
 * Must be unique per each script project.
 * Prevents collisions of multiple `class Program` declarations.
 * Will be used to detect the ingame script region, whose name is the same.
 */
namespace Needle_Cart_V1
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
        #region Needle_Cart_V1

        //GLOBALS
        Dictionary<string, string> Blocks = new Dictionary<string, string>() {
            {"Controller", "Needle Cart - PB - Controller"},
            {"Brake", "Needle Cart - Remote Control - Brake"},
            {"Wheel", "Needle Cart - Offroad Wheel Suspension 3x3"},
            {"Connector", "Needle Cart - Connector"},
            {"Camera", "Needle Cart - Camera"},
            {"Door", "Needle Cart - Door"},
            {"LCD", "Needle Cart - LCD Panel - Control Stats"},
            {"Battery", "Needle Cart - Warfare Battery"}
        };

        //DATA
        string Direction;
        float TargetSpeed;
        float CurrentSpeed;
        bool EmergencyStop;
        MyDetectedEntityInfo hitInfo;

        IMyCubeGrid grid;
        IMyTerminalBlock Controller;
        List<IMyTerminalBlock> Brakes = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> Wheels = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> Cameras = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> Connectors = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> Doors = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> LCDs = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> Batteries = new List<IMyTerminalBlock>();


        //METHODS
        public void GetBlocks()
        {
            grid = Me.CubeGrid as IMyCubeGrid;
            Controller = GridTerminalSystem.GetBlockWithName(Blocks["Controller"]);
            GridTerminalSystem.SearchBlocksOfName(Blocks["Brake"], Brakes);
            GridTerminalSystem.SearchBlocksOfName(Blocks["Wheel"], Wheels);
            GridTerminalSystem.SearchBlocksOfName(Blocks["Connector"], Connectors);
            GridTerminalSystem.SearchBlocksOfName(Blocks["Camera"], Cameras);
            GridTerminalSystem.SearchBlocksOfName(Blocks["Door"], Doors);
            GridTerminalSystem.SearchBlocksOfName(Blocks["LCD"], LCDs);
            GridTerminalSystem.SearchBlocksOfName(Blocks["Battery"], Batteries);
        }

        public void GetCustomData()
        {
            string[] CustomData = Controller.CustomData.Split('\n');
            Dictionary<string, string> data = new Dictionary<string, string>();

            if (CustomData[0] == "")
            {
                SetCustomData();
            }
            else
            {
                foreach (string line in CustomData)
                {
                    string[] part = line.Split(':');

                    if (!part[0].Contains('#') && part[0].Trim() != "")
                    {
                        data.Add(part[0].Trim().Replace(' ', '_').ToLower(), part[1].Trim().ToLower());
                    }
                }
            }

            Direction = data["direction"];
            EmergencyStop = bool.Parse(data["emergency_stop"]);
        }

        public void SetCustomData()
        {
            string[] CustomData = Controller.CustomData.Split('\n');
            if (CustomData[0] == "")
            {
                Controller.CustomData = (
                    "#####DATA - DON'T TOUCH#####\n" +
                    "Direction: down\n" +
                    "Emergency Stop: False"
                );

                GetCustomData();
            }
            else
            {
                Controller.CustomData = (
                    "#####DATA - DON'T TOUCH#####\n" +
                    "Direction: " + Direction + "\n" +
                    "Emergency Stop: " + EmergencyStop.ToString()
                );
            }
        }

        public void ApplyActionToAll(List<IMyTerminalBlock> Blocks, string Action, string filter = "")
        {
            foreach (IMyTerminalBlock Block in Blocks)
            {
                if (Block.CustomName.Contains(filter))
                {
                    Block.ApplyAction(Action);
                }
            }
        }

        public void SetDirection()
        {
            float TargetDirection = Direction == "up" ? -100f : 100f;
            foreach (IMyTerminalBlock Wheel in Wheels)
            {
                Wheel.SetValueFloat("Propulsion override", TargetDirection);
            }
        }

        public void CheckEmergencyStop()
        {
            foreach (IMyRemoteControl Brake in Brakes)
            {
                if (EmergencyStop)
                {
                    TargetSpeed = 0;
                    Brake.HandBrake = true;
                }
            }
        }

        public void GetTargetSpeed()
        {
            IMyCameraBlock Camera = Cameras.Find(Cam => Cam.CustomName.ToString().ToLower().Contains(Direction)) as IMyCameraBlock;
            Camera.EnableRaycast = true;

            if (Camera.AvailableScanRange > 150 && Camera.EnableRaycast)
            {
                hitInfo = Camera.Raycast(Camera.AvailableScanRange);

                if (hitInfo.IsEmpty())
                {
                    TargetSpeed = 85f;
                }
                else
                {
                    ApplyActionToAll(Connectors, "OnOff_On");
                    float distance = (float)Vector3D.Distance((Vector3D)hitInfo.HitPosition, Camera.GetPosition());
                    TargetSpeed = (float)(distance * 0.25);
                }
                Camera.EnableRaycast = false;
            }
        }

        public void CheckSpeed()
        {
            IMyRemoteControl SpeedSource = Brakes[0] as IMyRemoteControl;
            CurrentSpeed = (float)SpeedSource.GetShipSpeed();
            foreach (IMyRemoteControl Brake in Brakes)
            {
                Brake.HandBrake = CurrentSpeed > TargetSpeed ? true : false;
            }
        }

        public void Dock()
        {
            if (CurrentSpeed < 0.15f && TargetSpeed < 10f)
            {
                ApplyActionToAll(Connectors, "Lock");

                bool Locked = true;

                foreach (IMyShipConnector Connector in Connectors)
                {
                    if (Connector.Status.ToString() != "Connected" && Connector.CustomName.ToString().ToLower().Contains(Direction))
                    {
                        Locked = false;
                    }
                }

                if (Locked)
                {
                    ApplyActionToAll(Doors, "OnOff_On");
                    SetBatteryCharge(true);
                }
            }
        }

        public void Undock()
        {
            SetBatteryCharge(false);
            ApplyActionToAll(Connectors, "Unlock");
            ApplyActionToAll(Connectors, "OnOff_Off");
            ApplyActionToAll(Doors, "OnOff_Off");
        }

        public void SetBatteryCharge(bool Recharge)
        {
            foreach (IMyBatteryBlock Battery in Batteries)
            {
                Battery.ChargeMode = Recharge ? ChargeMode.Recharge : ChargeMode.Auto;
            }
        }

        public void UpdateLCDs()
        {
            string TextOutput = "### Needle Cart ###\n";
            TextOutput += "\n";
            TextOutput += "Trip Details:\n";
            TextOutput += "\n";
            TextOutput += "Heading: " + Direction.ToUpper() + "\n";
            TextOutput += "Target Speed: " + TargetSpeed.ToString("0.00") + "\n";
            TextOutput += "Current Speed: " + CurrentSpeed.ToString("0.00") + "\n";
            TextOutput += "\n";
            TextOutput += "### Have A Safe Trip ###";

            // Echo(TextOutput);
            foreach (IMyTextSurface LCD in LCDs)
            {
                LCD.ContentType = ContentType.TEXT_AND_IMAGE;
                LCD.FontSize = 1.5f;
                LCD.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
                LCD.WriteText(TextOutput);
            }
        }

        public void ArgInput(string args)
        {
            switch (args.ToLower())
            {
                case "up":
                    EmergencyStop = false;
                    Direction = "up";
                    Undock();
                    break;
                case "down":
                    EmergencyStop = false;
                    Direction = "down";
                    Undock();
                    break;
                case "stop":
                    EmergencyStop = true;
                    break;
            }

            SetCustomData();
        }

        //UPDATE FREQUENCY
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        //MAIN
        public void Main(string args)
        {
            GetBlocks();
            GetCustomData();
            ArgInput(args);
            CheckEmergencyStop();
            if (!EmergencyStop)
            {
                SetDirection();
                GetTargetSpeed();
                CheckSpeed();
                Dock();
            }
            UpdateLCDs();
        }

        #endregion // Needle_Cart_V1
    }
}