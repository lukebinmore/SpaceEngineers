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
namespace Needle_V3
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
        #region Needle_V3

        //GLOBALS
        Dictionary<string, string> Blocks = new Dictionary<string, string>() {
            {"Controller", "Needle - PB - Controller"},
            {"Remote", "Needle - Altitude Remote"},
            {"HingeTop", "Needle - Hinge - Top"},
            {"HingeBottom", "Needle - Hinge - Bottom"},
            {"Piston", "Needle - Piston"},
            {"Welder", "Needle - Welder"},
            {"Connector", "Needle - Connector - Joint"},
            {"ProjectorNN", "Needle - Projector - Normal Normal"},
            {"ProjectorNJ", "Needle - Projector - Normal Joint"},
            {"ProjectorJN", "Needle - Projector - Joint Normal"},
            {"RHPProjectorN", "Needle - Projector - Remove Hinge Part Normal"},
            {"RHPProjectorJ", "Needle - Projector - Remove Hinge Part Joint"},
            {"RHPGrinder", "Needle - Grinder - Remove Hinge Part"},
            {"RHPSensor", "Needle - Sensor - Remove Hinge Part"}
        };

        //DATA
        float PistonSpeed;
        int Stage;
        bool Running;
        bool SpaceReached;
        int JointDistance;
        int NextJointHeight;
        int NextSection;
        List<IMyTerminalBlock> Pistons = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> Welders = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> Connectors = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> Grinders = new List<IMyTerminalBlock>();

        IMyCubeGrid grid;
        IMyTerminalBlock Controller;
        IMyRemoteControl Remote;
        IMyMotorStator HingeTop;
        IMyMotorStator HingeBottom;
        IMyProjector ProjectorNN;
        IMyProjector ProjectorNJ;
        IMyProjector ProjectorJN;
        IMyProjector RHPProjectorN;
        IMyProjector RHPProjectorJ;
        IMySensorBlock RHPSensor;



        //METHODS
        public void GetBlocks()
        {
            grid = Me.CubeGrid as IMyCubeGrid;
            Controller = GridTerminalSystem.GetBlockWithName(Blocks["Controller"]);
            Remote = GridTerminalSystem.GetBlockWithName(Blocks["Remote"]) as IMyRemoteControl;
            HingeTop = GridTerminalSystem.GetBlockWithName(Blocks["HingeTop"]) as IMyMotorStator;
            HingeBottom = GridTerminalSystem.GetBlockWithName(Blocks["HingeBottom"]) as IMyMotorStator;
            ProjectorNN = GridTerminalSystem.GetBlockWithName(Blocks["ProjectorNN"]) as IMyProjector;
            ProjectorNJ = GridTerminalSystem.GetBlockWithName(Blocks["ProjectorNJ"]) as IMyProjector;
            ProjectorJN = GridTerminalSystem.GetBlockWithName(Blocks["ProjectorJN"]) as IMyProjector;
            RHPProjectorN = GridTerminalSystem.GetBlockWithName(Blocks["RHPProjectorN"]) as IMyProjector;
            RHPProjectorJ = GridTerminalSystem.GetBlockWithName(Blocks["RHPProjectorJ"]) as IMyProjector;
            RHPSensor = GridTerminalSystem.GetBlockWithName(Blocks["RHPSensor"]) as IMySensorBlock;
            GridTerminalSystem.SearchBlocksOfName(Blocks["Piston"], Pistons);
            GridTerminalSystem.SearchBlocksOfName(Blocks["Welder"], Welders);
            GridTerminalSystem.SearchBlocksOfName(Blocks["RHPGrinder"], Grinders);
            GridTerminalSystem.SearchBlocksOfName(Blocks["Connector"], Connectors);
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

                PistonSpeed = float.Parse(data["Piston_Speed"]);
                Stage = Int32.Parse(data["Stage"]);
                Running = bool.Parse(data["Running"].ToLower());
                SpaceReached = bool.Parse(data["Space_Reached"].ToLower());
                NextJointHeight = Int32.Parse(data["Next_Joint_Height"]);
                NextSection = Int32.Parse(data["Next_Section"]);
                JointDistance = Int32.Parse(data["Joint_Distance"]);
            }
        }

        public void SetCustomData()
        {
            string[] CustomData = Controller.CustomData.Split('\n');
            if (CustomData[0] == "")
            {
                Controller.CustomData = (
                    "#####SETTINGS#####\n" +
                    "Piston Speed: 0.5\n" +
                    "Joint Distance: 5000\n" +
                    "#####DATA - DON'T TOUCH#####\n" +
                    "Stage: 1\n" +
                    "Running: False\n" +
                    "Space Reached: False\n" +
                    "Next Joint Height: 1000\n" +
                    "Next Section: 0"
                );

                GetCustomData();
            }
            else
            {
                Controller.CustomData = (
                    "#####SETTINGS#####\n" +
                    "Piston Speed: " + PistonSpeed + "\n" +
                    "Joint Distance: " + JointDistance + "\n" +
                    "#####DATA - DON'T TOUCH#####\n" +
                    "Stage: " + Stage + "\n" +
                    "Running: " + Running.ToString() + "\n" +
                    "Space Reached: " + SpaceReached.ToString() + "\n" +
                    "Next Joint Height: " + NextJointHeight + "\n" +
                    "Next Section: " + NextSection
                );
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

        public void SetPistonVelocity(float Speed)
        {
            foreach (IMyTerminalBlock Piston in Pistons)
            {
                Piston.SetValue<float>("Velocity", Speed);
            }
        }

        public void ApplyActionToAll(List<IMyTerminalBlock> Blocks, string Action)
        {
            foreach (IMyTerminalBlock Block in Blocks)
            {
                Block.ApplyAction(Action);
            }
        }

        public void ReachedSpaceCheck()
        {
            double CurrGrav = Remote.GetNaturalGravity().Length() / 9.81;
            if (CurrGrav == 0)
            {
                SpaceReached = true;
            }
        }

        public void ArgInput(string arg)
        {
            switch (arg.ToLower())
            {
                case "start":
                    Running = true;
                    break;
                case "stop":
                    Running = false;
                    break;
            }
        }

        public void DisableProjectors()
        {
            ProjectorNN.ApplyAction("OnOff_Off");
            ProjectorNJ.ApplyAction("OnOff_Off");
            ProjectorJN.ApplyAction("OnOff_Off");
            RHPProjectorN.ApplyAction("OnOff_Off");
            RHPProjectorJ.ApplyAction("OnOff_Off");
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
            ReachedSpaceCheck();
            ArgInput(args);

            if (Running)
            {
                Echo("Running Program\n");

                if (Stage == 1)
                {
                    if (SpaceReached)
                    {
                        Echo("Space Already Reached!!!");
                        Running = false;
                        ApplyActionToAll(Welders, "OnOff_Off");
                    }
                    else
                    {
                        Echo("Activating Welders");

                        ApplyActionToAll(Welders, "OnOff_On");
                        Stage = 2;
                    }
                }
                else if (Stage == 2)
                {
                    Echo("Checking Height");

                    MyPlanetElevation Elevation = MyPlanetElevation.Surface;
                    double Altitude = 0;
                    Remote.TryGetPlanetElevation(Elevation, out Altitude);
                    if (double.IsInfinity(Altitude))
                    {
                        Altitude = 0;
                    }

                    if (NextSection == 0)
                    {
                        if (Altitude >= NextJointHeight)
                        {
                            NextSection = 1;
                            NextJointHeight += JointDistance;
                        }
                    }

                    Stage = 3;
                }
                else if (Stage == 3)
                {

                    IMyProjector Projector;
                    switch (NextSection)
                    {
                        case 1:
                            Echo("Printing Normal To Joint Section");
                            Projector = ProjectorNJ as IMyProjector;
                            break;
                        case 2:
                            Echo("Printing Joint To Normal Section");
                            Projector = ProjectorJN as IMyProjector;
                            break;
                        default:
                            Echo("Printing Normal Section");
                            Projector = ProjectorNN as IMyProjector;
                            break;
                    }

                    Projector.ApplyAction("OnOff_On");
                    HingeTop.ApplyAction("Detach");

                    if (CheckPistonState("Extended"))
                    {
                        if (Projector.RemainingBlocks == 0)
                        {
                            HingeTop.ApplyAction("Attach");
                            if (HingeTop.IsAttached)
                            {
                                Stage = 4;
                            }
                        }
                        else
                        {
                            SetPistonVelocity(-PistonSpeed);
                        }
                    }
                    else if (Projector.RemainingBlocks == 0 || CheckPistonState("Retracted"))
                    {
                        SetPistonVelocity(PistonSpeed);
                    }
                }
                else if (Stage == 4)
                {
                    Echo("Re-Staging Rear");

                    DisableProjectors();
                    HingeBottom.ApplyAction("Detach");
                    SetPistonVelocity(-2.5f);
                    if (CheckPistonState("Retracted"))
                    {
                        HingeBottom.ApplyAction("Attach");
                        if (HingeBottom.IsAttached)
                        {
                            Stage = 5;
                        }
                    }
                }
                else if (Stage == 5)
                {
                    Echo("Removing Bottom Hinge Part");

                    IMyProjector Projector;

                    if (NextSection == 2)
                    {
                        Projector = RHPProjectorJ as IMyProjector;
                    }
                    else
                    {
                        Projector = RHPProjectorN as IMyProjector;
                    }

                    Projector.ApplyAction("OnOff_On");
                    ApplyActionToAll(Welders, "OnOff_Off");
                    ApplyActionToAll(Grinders, "OnOff_On");

                    if (!RHPSensor.IsActive)
                    {
                        ApplyActionToAll(Welders, "OnOff_On");
                        ApplyActionToAll(Grinders, "OnOff_Off");
                        if (Projector.RemainingBlocks == 0)
                        {
                            DisableProjectors();
                            Stage = 6;
                        }
                    }
                }
                else if (Stage == 6)
                {
                    Echo("Checking Next Section");

                    switch (NextSection)
                    {
                        case 1:
                            NextSection = 2;
                            break;
                        case 2:
                            NextSection = 0;
                            foreach (IMyTerminalBlock Connector in Connectors)
                            {
                                Connector.ApplyAction("Lock");
                            }
                            break;
                    }

                    Stage = 1;
                }
            }
            else
            {
                Echo("Standing By");
            }

            SetCustomData();
        }

        #endregion // Needle_V3
    }
}