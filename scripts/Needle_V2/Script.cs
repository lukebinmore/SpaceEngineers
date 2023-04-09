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
namespace Needle_V2
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
        #region Needle_V2

        //GLOBALS
        Dictionary<string, string> Names = new Dictionary<string, string>() {
            {"Controller", "Needle - PB - Controller"},
            {"Projector", "Needle - Projector - Main"},
            {"ProjectorTip", "Needle - Projector - Tip"},
            {"RemoteControl", "Needle - Remote Control"},
            {"Piston", "Needle - Piston"},
            {"Welder", "Needle - Welder"},
            {"BRotors", "Needle - Advanced Rotor - Bottom"},
            {"TRotors", "Needle - Advanced Rotor - Top"},
            {"Lights", "Needle - Warning Light"},
            {"Connector", "Needle - Connector"},
            {"SAMController", "Needle - PB - [SAM ADVERTISE"}
        };

        //DATA
        float PistonSpeed;
        int Stage;
        int AdditionalRuns;
        bool Running;
        bool SpaceReached;
        string PlanetName;
        List<IMyTerminalBlock> Pistons = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> Welders = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> BRotors = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> TRotors = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> WarningLights = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> Connectors = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> SAMController = new List<IMyTerminalBlock>();
        IMyCubeGrid grid;
        IMyTerminalBlock Controller;
        IMyProjector Projector;
        IMyProjector ProjectorTip;
        IMyRemoteControl RemoteControl;
        IMyTextSurface LCD;



        //METHODS
        public void GetBlocks()
        {
            grid = Me.CubeGrid as IMyCubeGrid;
            Controller = GridTerminalSystem.GetBlockWithName(Names["Controller"]);
            Projector = GridTerminalSystem.GetBlockWithName(Names["Projector"]) as IMyProjector;
            ProjectorTip = GridTerminalSystem.GetBlockWithName(Names["ProjectorTip"]) as IMyProjector;
            RemoteControl = GridTerminalSystem.GetBlockWithName(Names["RemoteControl"]) as IMyRemoteControl;
            LCD = Me.GetSurface(0);
            GridTerminalSystem.SearchBlocksOfName(Names["Piston"], Pistons);
            GridTerminalSystem.SearchBlocksOfName(Names["Welder"], Welders);
            GridTerminalSystem.SearchBlocksOfName(Names["BRotors"], BRotors);
            GridTerminalSystem.SearchBlocksOfName(Names["TRotors"], TRotors);
            GridTerminalSystem.SearchBlocksOfName(Names["Lights"], WarningLights);
            GridTerminalSystem.SearchBlocksOfName(Names["Connector"], Connectors);
            GridTerminalSystem.SearchBlocksOfName(Names["SAMController"], SAMController);
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

                    if (!part[0].Contains('#'))
                    {
                        data.Add(part[0].Trim().Replace(' ', '_'), part[1].Trim());
                    }
                    else
                    {
                        data.Add(part[0].Trim().Replace(' ', '_'), "");
                    }
                }

                PlanetName = data["Planet_Name"];
                PistonSpeed = float.Parse(data["Piston_Speed"]);
                AdditionalRuns = Int32.Parse(data["Additional_Runs"]);
                Stage = Int32.Parse(data["Stage"]);
                Running = bool.Parse(data["Running"].ToLower());
                SpaceReached = bool.Parse(data["Space_Reached"].ToLower());
            }
        }

        public void SetCustomData()
        {
            string[] CustomData = Controller.CustomData.Split('\n');
            if (CustomData[0] == "")
            {
                Controller.CustomData = (
                    "#####SETTINGS#####\n" +
                    "Planet Name: Earth\n" +
                    "Piston Speed: 0.5\n" +
                    "Additional Runs: 2\n" +
                    "#####DATA - DON'T TOUCH#####\n" +
                    "Stage: 1\n" +
                    "Running: False\n" +
                    "Space Reached: False"
                );

                GetCustomData();
            }
            else
            {
                Controller.CustomData = (
                    "#####SETTINGS#####\n" +
                    "Planet Name: " + PlanetName + "\n" +
                    "Piston Speed: " + PistonSpeed + "\n" +
                    "Additional Runs: " + AdditionalRuns + "\n" +
                    "#####DATA - DON'T TOUCH#####\n" +
                    "Stage: " + Stage + "\n" +
                    "Running: " + Running.ToString() + "\n" +
                    "Space Reached: " + SpaceReached
                );
            }
        }

        public void UpdatePlanetName()
        {
            foreach (IMyTerminalBlock Connector in Connectors)
            {
                string[] NameParts = Connector.CustomName.Split('-');

                Connector.CustomData = "SAM.Name=" + NameParts[2].Trim();
            }

            string[] ControllerParts = SAMController[0].CustomName.Split('-');
            SAMController[0].CustomName = ControllerParts[0] + "-" + ControllerParts[1] + "- [SAM ADVERTISE Name=" + PlanetName + " Needle]";
        }

        public void ApplyActionToAll(List<IMyTerminalBlock> Blocks, string Action)
        {
            foreach (IMyTerminalBlock Block in Blocks)
            {
                Block.ApplyAction(Action);
            }
        }

        public bool CheckPistonState(string Desired)
        {
            bool PistonMatch = true;
            foreach (IMyTerminalBlock Piston in Pistons)
            {
                var PistonTemp = Piston as IMyPistonBase;

                if (PistonTemp.Status.ToString() != Desired)
                {
                    PistonMatch = false;
                }
            }

            if (PistonMatch)
            {
                return true;
            }

            return false;
        }

        public bool CheckRotorAttach(List<IMyTerminalBlock> Blocks, string Desired)
        {
            bool Attached = true;
            bool Detached = true;

            foreach (IMyTerminalBlock Rotor in Blocks)
            {
                var RotorTemp = Rotor as IMyMotorStator;
                if (RotorTemp.IsAttached)
                {
                    Detached = false;
                }
                else
                {
                    Attached = false;
                }
            }

            if (Desired == "Attached")
            {
                return Attached;
            }
            else if (Desired == "Detached")
            {
                return Detached;
            }

            return false;
        }

        public void SetPistonVelocity(float Speed)
        {
            foreach (IMyTerminalBlock Piston in Pistons)
            {
                Piston.SetValue<float>("Velocity", Speed);
            }
        }

        public void ReachedSpaceCheck()
        {
            double CurrGrav = RemoteControl.GetNaturalGravity().Length() / 9.81;
            if (CurrGrav == 0)
            {
                SpaceReached = true;
            }
        }

        public bool CheckCompletedLink()
        {
            if (Projector.RemainingBlocks == 0)
            {
                return true;
            }

            return false;
        }

        public void UpdateScreens(string TextInput, string ProjectorToUse = "Projector")
        {
            string TextOutput = "#####";

            if (Running)
            {
                TextOutput += "RUNNING";
            }
            else
            {
                TextOutput += "WAITING";
            };

            TextOutput += (
                "#####\n" +
                "\n" +
                TextInput + "\n" +
                "Current Stage: " + Stage.ToString() + "\n"
            );

            if (ProjectorToUse == "Projector")
            {
                TextOutput += "Remaining Blocks: " + Projector.RemainingBlocks.ToString() + "\n";
            }
            else if (ProjectorToUse == "ProjectorTip")
            {
                TextOutput += "Remaining Blocks: " + ProjectorTip.RemainingBlocks.ToString() + "\n";
            }

            TextOutput += "Space Reached: " + SpaceReached.ToString();

            LCD.ContentType = ContentType.TEXT_AND_IMAGE;
            LCD.FontSize = 1.5f;
            LCD.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
            LCD.WriteText(TextOutput);
            Echo(TextOutput);
        }

        public void ArgInput(string arg)
        {
            switch (arg.ToLower())
            {
                case "shipentry":
                    ApplyActionToAll(Pistons, "OnOff_Off");
                    break;
                case "shipexit":
                    ApplyActionToAll(Pistons, "OnOff_On");
                    break;
                case "start":
                    Running = true;
                    break;
                case "stop":
                    Running = false;
                    break;
            }
        }

        //UPDATE FREQUENCY
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        //MAIN
        public void Main(string args)
        {
            GetBlocks();
            GetCustomData();
            UpdatePlanetName();
            ReachedSpaceCheck();
            ArgInput(args);

            if (Running)
            {
                UpdateScreens("Running Program...");

                if (AdditionalRuns > 0)
                {
                    if (Stage == 1)
                    {
                        UpdateScreens("Activating Devices...");

                        ApplyActionToAll(WarningLights, "OnOff_On");
                        ApplyActionToAll(Welders, "OnOff_On");
                        Projector.ApplyAction("OnOff_On");

                        Stage = 2;
                    }
                    else if (Stage == 2)
                    {
                        UpdateScreens("Detatching Top Rotors...");

                        ApplyActionToAll(TRotors, "Detach");

                        if (CheckRotorAttach(TRotors, "Detached"))
                        {
                            Stage = 3;
                        }
                    }
                    else if (Stage == 3)
                    {
                        UpdateScreens("Climbing...");

                        SetPistonVelocity(PistonSpeed);

                        if (CheckPistonState("Extended"))
                        {
                            if (CheckCompletedLink())
                            {
                                Stage = 4;
                            }
                            else
                            {
                                Stage = -4;
                            }
                        }
                    }
                    else if (Stage == 4)
                    {
                        UpdateScreens("Connecting Top Rotors...");

                        ApplyActionToAll(TRotors, "Attach");

                        if (CheckRotorAttach(TRotors, "Attached"))
                        {
                            Stage = 5;
                            Projector.ApplyAction("OnOff_Off");
                        }
                    }
                    else if (Stage == 5)
                    {
                        UpdateScreens("Disconnecting Bottom Rotors...");

                        ApplyActionToAll(BRotors, "Detach");

                        if (CheckRotorAttach(BRotors, "Detached"))
                        {
                            Stage = 6;
                        }
                    }
                    else if (Stage == 6)
                    {
                        UpdateScreens("Re-Staging Rear...");

                        SetPistonVelocity(-1);

                        if (CheckPistonState("Retracted"))
                        {
                            Stage = 7;
                        }
                    }
                    else if (Stage == 7)
                    {
                        UpdateScreens("Connecting Bottom Connectors...");

                        ApplyActionToAll(BRotors, "Attach");

                        if (CheckRotorAttach(BRotors, "Attached"))
                        {
                            Stage = 1;

                            if (SpaceReached)
                            {
                                AdditionalRuns -= 1;
                            }
                        }
                    }
                    else if (Stage == -4)
                    {
                        UpdateScreens("INCOMPLETE SECTION!\nDisconnecting Top Rotors...");

                        ApplyActionToAll(TRotors, "Detach");

                        if (CheckRotorAttach(TRotors, "Detached"))
                        {
                            Stage = -5;
                        }
                    }
                    else if (Stage == -5)
                    {
                        UpdateScreens("INCOMPLETE SECTION!\nReturning To Complete...");

                        SetPistonVelocity(-PistonSpeed);

                        if (CheckPistonState("Retracted") || Projector.RemainingBlocks == 0)
                        {
                            Stage = 3;
                        }
                    }
                }
                else
                {
                    UpdateScreens("Tower Finished!\nBuilding Platform...", "ProjectorTip");

                    ProjectorTip.ApplyAction("OnOff_On");
                    ApplyActionToAll(WarningLights, "OnOff_On");
                    ApplyActionToAll(Welders, "OnOff_On");

                    if (ProjectorTip.RemainingBlocks == 0)
                    {
                        UpdateScreens("Construction Finished!");
                        ApplyActionToAll(WarningLights, "OnOff_Off");
                        ApplyActionToAll(Welders, "OnOff_Off");

                        Running = false;
                    }
                }
            }
            else
            {
                UpdateScreens("Standing By");
            }

            SetCustomData();
        }

        #endregion // Needle_V2
    }
}