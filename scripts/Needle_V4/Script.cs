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
using Microsoft.VisualBasic;
using System.Security;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using Sandbox.Definitions;
using System.Net.Http.Headers;

/*
 * Must be unique per each script project.
 * Prevents collisions of multiple `class Program` declarations.
 * Will be used to detect the ingame script region, whose name is the same.
 */
namespace Needle_V4
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
        #region Needle_V4

        //GLOBALS
        Dictionary<string, string> Blocks = new Dictionary<string, string>()
        {
            {"Controller", "Needle - PB - Controller"},
            {"Remote", "Needle - Remote Control - Gravity Detector"},
            {"Piston", "Needle - Piston"},
            {"TopRotor", "Needle - Advanced Rotor - Top"},
            {"BottomRotor", "Needle - Advanced Rotor - Bottom"},
            {"Joint", "Needle - Hinge - Joint"},
            {"Welder", "Needle - Welder"},
            {"Connector", "Needle - Connector - Joint"},
            {"ProjectorNormal", "Needle - Projector - Normal"},
            {"ProjectorJointA", "Needle - Projector - Joint A"},
            {"ProjectorJointB", "Needle - Projector - Joint B"},
            {"ProjectorJointTop", "Needle - Projector - Joint Top"},
            {"ProjectorPlatform", "Needle - Projector - Platform"},
            {"RemoteBase", "Needle - Remote Control - Base"}
    };

        float PistonSpeed;
        float PistonMax;
        float PistonMin;
        bool Running;
        bool SpaceReached;
        string Mode;
        string Direction;
        int Stage;
        string NextSection;
        int JointDistance;
        int NextJointHeight;
        double Altitude;
        double GroundAltitude;
        List<IMyTerminalBlock> Pistons = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> Welders = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> TopRotors = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> BottomRotors = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> Hinges = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> Connectors = new List<IMyTerminalBlock>();
        IMyCubeGrid grid;
        IMyTerminalBlock Controller;
        IMyRemoteControl Remote;
        IMyRemoteControl RemoteBase;
        IMyProjector ProjectorNormal;
        IMyProjector ProjectorJointA;
        IMyProjector ProjectorJointB;
        IMyProjector ProjectorJointTop;
        IMyProjector ProjectorPlatform;
        IMyProjector Projector;

        //METHODS
        public void GetBlocks()
        {
            grid = Me.CubeGrid as IMyCubeGrid;
            Controller = GridTerminalSystem.GetBlockWithName(Blocks["Controller"]);
            CheckMissing(Controller, "Controller");
            Remote = GridTerminalSystem.GetBlockWithName(Blocks["Remote"]) as IMyRemoteControl;
            CheckMissing(Remote, "Gravity Detector");
            ProjectorNormal = GridTerminalSystem.GetBlockWithName(Blocks["ProjectorNormal"]) as IMyProjector;
            CheckMissing(ProjectorNormal, "Normal Projector");
            ProjectorJointA = GridTerminalSystem.GetBlockWithName(Blocks["ProjectorJointA"]) as IMyProjector;
            CheckMissing(ProjectorJointA, "Joint A Projector");
            ProjectorJointB = GridTerminalSystem.GetBlockWithName(Blocks["ProjectorJointB"]) as IMyProjector;
            CheckMissing(ProjectorJointB, "Joint B Projector");
            ProjectorJointTop = GridTerminalSystem.GetBlockWithName(Blocks["ProjectorJointTop"]) as IMyProjector;
            CheckMissing(ProjectorJointTop, "Top Joint Projector");
            ProjectorPlatform = GridTerminalSystem.GetBlockWithName(Blocks["ProjectorPlatform"]) as IMyProjector;
            CheckMissing(ProjectorPlatform, "Platform Projector");
            RemoteBase = GridTerminalSystem.GetBlockWithName(Blocks["RemoteBase"]) as IMyRemoteControl;
            CheckMissing(RemoteBase, "Base Remote Control");
            GridTerminalSystem.SearchBlocksOfName(Blocks["Piston"], Pistons);
            CheckMissing(Pistons, "Pistons");
            GridTerminalSystem.SearchBlocksOfName(Blocks["Welder"], Welders);
            CheckMissing(Welders, "Welders");
            GridTerminalSystem.SearchBlocksOfName(Blocks["TopRotor"], TopRotors);
            CheckMissing(TopRotors, "Top Inchworm Rotors");
            GridTerminalSystem.SearchBlocksOfName(Blocks["BottomRotor"], BottomRotors);
            CheckMissing(BottomRotors, "Bottom Inchworm Rotors");
            GridTerminalSystem.SearchBlocksOfName(Blocks["Joint"], Hinges);
            GridTerminalSystem.SearchBlocksOfName(Blocks["Connector"], Connectors);
        }

        public void CheckMissing<T>(T Obj, string Name)
        {
            try
            {
                if (Obj == null)
                {
                    throw new Exception($"Error: {Name} could not be found!");
                }
            }
            catch (Exception ex)
            {
                Echo(ex.Message);
            }
        }

        public void GetCustomData()
        {
            string[] CustomData = Controller.CustomData.Split('\n');

            if (CustomData[0] == "")
            {
                //SetCustomData();
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
                }

                PistonSpeed = float.Parse(data["Piston_Speed"]);
                PistonMax = float.Parse(data["Piston_Max_Extent"]);
                PistonMin = float.Parse(data["Piston_Min_Extent"]);
                Running = bool.Parse(data["Running"]);
                SpaceReached = bool.Parse(data["Space_Reached"]);
                Stage = Int32.Parse(data["Stage"]);
                Mode = data["Mode"];
                Direction = data["Direction"];
                NextSection = data["Next_Section"];
                JointDistance = Int32.Parse(data["Joint_Distance"]);
                NextJointHeight = Int32.Parse(data["Next_Joint_Height"]);
                GroundAltitude = double.Parse(data["Ground_Altitude"]);
            }
        }

        public void SetCustomData()
        {
            string[] CustomData = Controller.CustomData.Split('\n');
            if (CustomData[0] == "")
            {
                Controller.CustomData = (
                    "#####SETTINGS#####\n" +
                    "Piston Speed: 1\n" +
                    "Piston Max Extent: 9.340\n" +
                    "Piston Min Extent: 0.3400\n" +
                    "Joint Distance: 5000\n" +
                    "Ground Altitude: 30\n" +
                    "#####DATA - DON'T TOUCH#####\n" +
                    "Stage: 1\n" +
                    "Running: False\n" +
                    "Space Reached: False\n" +
                    "Mode: Idle\n" +
                    "Direction: Accending\n" +
                    "Next Section: NA\n" +
                    "Next Joint Height: 5000"
                );

                GetCustomData();
            }
            else
            {
                Controller.CustomData = (
                    "#####SETTINGS#####\n" +
                    $"Piston Speed: {PistonSpeed}\n" +
                    $"Piston Max Extent: {PistonMax}\n" +
                    $"Piston Min Extent: {PistonMin}\n" +
                    $"Joint Distance: {JointDistance}\n" +
                    $"Ground Altitude: {GroundAltitude}\n" +
                    "#####DATA - DON'T TOUCH#####\n" +
                    $"Stage: {Stage}\n" +
                    $"Running: {Running.ToString()}\n" +
                    $"Space Reached: {SpaceReached.ToString()}\n" +
                    $"Mode: {Mode}\n" +
                    $"Direction: {Direction}\n" +
                    $"Next Section: {NextSection}\n" +
                    $"Next Joint Height: {NextJointHeight}"
                );
            }
        }

        public bool CheckPistonState(string Desired)
        {
            bool StateMatch = true;
            foreach (IMyPistonBase Piston in Pistons)
            {
                if (Piston.Status.ToString() != Desired)
                {
                    StateMatch = false;
                }
            }

            return StateMatch;
        }

        public void SetPistonVelocity(float Speed)
        {
            foreach (IMyPistonBase Piston in Pistons)
            {
                Piston.SetValue<float>("Velocity", Speed);
            }
        }

        public void SetPistonLimits(float Min, float Max)
        {
            foreach (IMyPistonBase Piston in Pistons)
            {
                Piston.MinLimit = Min;
                Piston.MaxLimit = Max;
            }
        }

        public void ApplyActionToAll(List<IMyTerminalBlock> Blocks, string Action)
        {
            foreach (IMyTerminalBlock Block in Blocks)
            {
                Block.ApplyAction(Action);
            }
        }

        public bool CheckRotorsAttached(List<IMyTerminalBlock> Rotors)
        {
            bool Attached = true;
            foreach (IMyMotorBase Rotor in Rotors)
            {
                if (!Rotor.IsAttached)
                {
                    Attached = false;
                }
            }

            return Attached;
        }

        public bool CheckRotorsDettached(List<IMyTerminalBlock> Rotors)
        {
            bool Dettached = true;
            foreach (IMyMotorBase Rotor in Rotors)
            {
                if (Rotor.IsAttached)
                {
                    Dettached = false;
                }
            }

            return Dettached;
        }

        public bool CheckConnectorsLocked()
        {
            bool Locked = true;
            foreach (IMyShipConnector Connector in Connectors)
            {
                if (Connector.Status != MyShipConnectorStatus.Connected)
                {
                    Locked = false;
                }
            }
            return Locked;
        }

        public void CheckSpaceReached()
        {
            double CurrGrav = Remote.GetNaturalGravity().Length() / 9.81;
            if (CurrGrav == 0)
            {
                SpaceReached = true;
            }
        }

        public void DisableProjectors()
        {
            Echo("Dissabaling Projectors...");
            ProjectorNormal.ApplyAction("OnOff_Off");
            ProjectorJointA.ApplyAction("OnOff_Off");
            ProjectorJointB.ApplyAction("OnOff_Off");
            ProjectorJointTop.ApplyAction("OnOff_Off");
            ProjectorPlatform.ApplyAction("OnOff_Off");
            Echo("Projectors Disabled!");
        }

        public bool CheckRunning()
        {
            if (Running)
            {
                ApplyActionToAll(Pistons, "OnOff_On");
                return true;
            }
            else
            {
                ApplyActionToAll(Pistons, "OnOff_Off");
                return false;
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
                case "construct":
                    Mode = "Construction";
                    break;
                case "repair":
                    Mode = "Repair";
                    break;
                case "up":
                    Direction = "Accending";
                    break;
                case "down":
                    Direction = "Deccending";
                    break;
                case "reset":
                    Mode = "Reset";
                    Running = true;
                    break;
            }
        }

        public void ConsolePrint()
        {
            Echo("Needle - MK_4");
            Echo("#####CURRENT STATUS#####");
            Echo($"Current Mode: {Mode}");
            Echo($"Curent Altitude: {Altitude}");
            Echo("#####COMMANDS#####");
            Echo("Start");
            Echo("Stop");
            Echo("Reset");
            Echo("#####MODES#####");
            Echo("Construct");
            Echo("Repair");
            Echo("");
        }

        public void GetCurrentAltitude()
        {
            Vector3D ConstructorPosition = Remote.GetPosition();
            Vector3D BasePosition = RemoteBase.GetPosition();
            Altitude = Vector3D.Distance(ConstructorPosition, BasePosition);
        }

        public bool CheckProjectionFinished(IMyProjector Projector)
        {
            if (Projector.RemainingBlocks == 0)
            {
                return true;
            }

            return false;
        }

        public void Reset()
        {
            Echo("Device Resetting...");
            DisableProjectors();
            if (Stage == 1)
            {
                Echo("Checking Bottom Rotors are Attached...");
                if (CheckRotorsAttached(BottomRotors))
                {
                    Echo("Attached!");
                    Stage = 2;
                }
                else
                {
                    Echo("Dettached, Checking Piston State...");
                    if (CheckPistonState("Extended") || CheckPistonState("Retracted"))
                    {
                        Echo("Pistons aligned, attempting Rotor Attachement...");
                        ApplyActionToAll(BottomRotors, "Attach");
                    }
                    else
                    {
                        Echo("Pistons not aligned! Retracting Pistons...");
                        SetPistonVelocity(-PistonSpeed);
                    }
                }
            }
            else if (Stage == 2)
            {
                Echo("Detaching Top Rotors...");
                ApplyActionToAll(TopRotors, "Detach");

                Echo("Checking Top Rotors are Detached...");
                if (CheckRotorsDettached(TopRotors))
                {
                    Echo("Rotors Detached!");
                    Stage = 3;
                }
            }
            else if (Stage == 3)
            {
                Echo("Retracting Pistons...");
                SetPistonVelocity(-PistonSpeed);
                Echo("Checking Piston State...");
                if (CheckPistonState("Retracted"))
                {
                    Echo("Pistons retracted, Attempting Rotor Attachement...");
                    ApplyActionToAll(TopRotors, "Attach");

                    Echo("Checking Top Rotors Attached...");
                    if (CheckRotorsAttached(TopRotors))
                    {
                        Echo("Rotors Attached!");
                        Stage = 4;
                    }
                }
            }
            else if (Stage == 4)
            {
                Echo("Disabling Components & Setting Defaults...");
                ApplyActionToAll(Pistons, "OnOff_Off");
                Echo("System Reset!");
                Mode = "Idle";
                Stage = 1;
                Running = false;
                NextJointHeight = JointDistance;
            }
        }

        public void Construct()
        {
            Echo("Constructing Tether...");

            switch (Stage)
            {
                case 1:
                    if (SpaceReached)
                    {
                        Echo("Space Already Reached!");
                        NextSection = "NA";
                        ProjectorPlatform.ApplyAction("OnOff_On");
                        Echo("Checking if Platform Complete...");
                        if (CheckProjectionFinished(ProjectorPlatform))
                        {
                            Echo("Projection Complete!, Disabling Components...");
                            DisableProjectors();
                            Running = false;
                            ApplyActionToAll(Welders, "OnOff_Off");
                            ApplyActionToAll(Pistons, "OnOff_Off");
                            Echo("Construction Complete!");
                        }
                        else
                        {
                            Echo("Constructing Platform...");
                        }
                    }
                    else
                    {
                        Echo("Activating Components...");
                        ApplyActionToAll(Welders, "OnOff_On");
                        ApplyActionToAll(Pistons, "OnOff_On");
                        Stage = 2;
                    }
                    break;
                case 2:
                    Echo("Checking Section...");
                    if (NextSection == "NA")
                    {
                        NextSection = "Normal";
                    }

                    if (NextSection == "Normal")
                    {
                        Echo("Checking Altitude...");
                        if (Altitude >= NextJointHeight)
                        {
                            NextSection = "Joint A";
                            NextJointHeight += JointDistance;
                        }
                    }
                    Stage = 3;
                    break;
                case 3:
                    Echo($"Activating {NextSection} Projector...");
                    if (CheckRotorsAttached(BottomRotors))
                    {
                        DisableProjectors();

                        try
                        {
                            switch (NextSection)
                            {
                                case "Normal":
                                    Projector = ProjectorNormal;
                                    break;
                                case "Joint A":
                                    Projector = ProjectorJointA;
                                    break;
                                case "Joint B":
                                    Projector = ProjectorJointB;
                                    break;
                                default:
                                    throw new Exception($"Error: Script missing next section, check Custom Data!");
                            }

                            Projector.ApplyAction("OnOff_On");

                            if (NextSection == "Joint B")
                            {
                                NextSection = "Normal";
                            }

                            Stage = 4;
                        }
                        catch (Exception ex)
                        {
                            Echo(ex.Message);
                        }
                    }
                    break;
                case 4:
                    Echo("Detaching Top Rotors...");
                    ApplyActionToAll(TopRotors, "Detach");

                    Echo("Extending Pistons...");
                    SetPistonVelocity(PistonSpeed);

                    Stage = 5;
                    break;
                case 5:
                    Echo("Checking Projection Progress...");
                    if (Projector == null)
                    {
                        Stage = 3;
                    }
                    else if (CheckProjectionFinished(Projector) & CheckPistonState("Extended"))
                    {
                        DisableProjectors();
                        Stage = 6;
                    }
                    else if (CheckPistonState("Extended"))
                    {
                        SetPistonVelocity(-PistonSpeed);
                    }
                    else if (CheckPistonState("Retracted"))
                    {
                        SetPistonVelocity(PistonSpeed);
                    }
                    break;
                case 6:
                    Echo("Checking for Joint Stage A...");

                    if (NextSection == "Joint A")
                    {
                        Echo("Expanding Pistons for Joint A Top...");
                        SetPistonLimits(PistonMax, 10);
                        Echo("Expanding Pistons...");
                        SetPistonVelocity(PistonSpeed);

                        if (CheckPistonState("Extended"))
                        {
                            Echo("Printing Joint Top...");
                            ProjectorJointTop.ApplyAction("OnOff_On");
                            Stage = 7;
                        }
                    }
                    else
                    {
                        Stage = 8;
                    }
                    break;
                case 7:
                    Echo("Checking Joint Top Progress...");
                    if (CheckProjectionFinished(ProjectorJointTop))
                    {
                        DisableProjectors();
                        Echo("Placing Joint Top...");
                        SetPistonVelocity(-PistonSpeed);

                        if (CheckPistonState("Retracted"))
                        {
                            Echo("Connecting Joint Top...");
                            ApplyActionToAll(Hinges, "Attach");

                            if (CheckRotorsAttached(Hinges))
                            {
                                ApplyActionToAll(Connectors, "Lock");
                                if (CheckConnectorsLocked())
                                {
                                    NextSection = "Joint B";
                                    Echo("Resetting Piston Limits...");
                                    SetPistonLimits(PistonMin, PistonMax);
                                    SetPistonVelocity(PistonSpeed);
                                    Stage = 8;
                                }
                            }
                        }
                    }
                    break;
                case 8:
                    Echo("Checking Pistons are fully extended...");
                    if (CheckPistonState("Extended"))
                    {
                        Echo("Attempting to Attach Top Rotors...");
                        ApplyActionToAll(TopRotors, "Attach");

                        if (CheckRotorsAttached(TopRotors))
                        {
                            Stage = 9;
                        }
                    }
                    break;
                case 9:
                    Echo("Detaching Bottom Rotors...");
                    ApplyActionToAll(BottomRotors, "Detach");

                    Echo("Restaging Rear...");
                    if (CheckRotorsDettached(BottomRotors))
                    {
                        SetPistonVelocity(-PistonSpeed);
                        Stage = 10;
                    }
                    break;
                case 10:
                    Echo("Checking Piston State...");
                    if (CheckPistonState("Retracted"))
                    {
                        Echo("Pistons retracted, attempting to Attach Bottom Rotors...");
                        ApplyActionToAll(BottomRotors, "Attach");

                        Echo("Checking Bottom Rotors...");
                        if (CheckRotorsAttached(BottomRotors))
                        {
                            Stage = 1;
                        }
                    }
                    break;
            }
        }

        public void Repair()
        {
            Echo("Repairing Tether...");

            if (Direction == "Accending")
            {
                if (SpaceReached)
                {
                    Echo("Reached top of Tether, heading down!");
                    Direction = "Deccending";
                }
                else
                {
                    switch (Stage)
                    {
                        case 1:
                            Echo("Activating Components...");
                            ApplyActionToAll(Welders, "OnOff_On");
                            ApplyActionToAll(Pistons, "OnOff_On");
                            SetPistonLimits(PistonMin, PistonMax);
                            Stage = 2;
                            break;
                        case 2:
                            Echo("Checking Bottom Rotors...");
                            if (CheckRotorsAttached(BottomRotors))
                            {
                                Echo("Rotors Attached!");
                                Stage = 3;
                            }
                            else
                            {
                                Echo("Rotors not attached, checking Pistons...");
                                if (CheckPistonState("Retracted"))
                                {
                                    Echo("Pistons retracted, attempting to attach Bottom Rotors...");
                                    ApplyActionToAll(BottomRotors, "Attach");
                                }
                                else
                                {
                                    Echo("Pistons not retracted, Retracting Pistons...");
                                    SetPistonVelocity(-PistonSpeed);
                                }
                            }
                            break;
                        case 3:
                            Echo("Detaching Top Rotors...");
                            ApplyActionToAll(TopRotors, "Detach");

                            Echo("Checking Top Rotors...");
                            if (CheckRotorsDettached(TopRotors))
                            {
                                Echo("Top Rotors Detached!");
                                Stage = 4;
                            }
                            break;
                        case 4:
                            Echo("Extending Pistons...");
                            SetPistonVelocity(PistonSpeed);

                            Echo("Checking if Pistons are Extended...");
                            if (CheckPistonState("Extended"))
                            {
                                Echo("Pistons Extended!");
                                Stage = 5;
                            }
                            break;
                        case 5:
                            Echo("Attaching Top Rotors...");
                            ApplyActionToAll(TopRotors, "Attach");

                            Echo("Checking Top Rotors...");
                            if (CheckRotorsAttached(TopRotors))
                            {
                                Echo("Top Rotors Attached!");
                                Stage = 5;
                            }
                            break;
                        case 6:
                            Echo("Detaching Bottom Rotors...");
                            ApplyActionToAll(BottomRotors, "Detach");

                            Echo("Checking Bottom Rotors...");
                            if (CheckRotorsAttached(BottomRotors))
                            {
                                Echo("Bottom Rotors Detached!");
                                Stage = 7;
                            }
                            break;
                        case 7:
                            Echo("Restaging Rear...");
                            SetPistonVelocity(-PistonSpeed);

                            Echo("Checking if Pistons are Retracted...");
                            if (CheckPistonState("Retracted"))
                            {
                                Echo("Pistons Retracted!");
                                Stage = 8;
                            }
                            break;
                        case 8:
                            Echo("Attaching Bottom Rotors...");
                            ApplyActionToAll(BottomRotors, "Attach");

                            Echo("Checking Bottom Rotors...");
                            if (CheckRotorsAttached(BottomRotors))
                            {
                                Echo("Bottom Rotors Attached!");
                                Stage = 1;
                            }
                            break;
                    }
                }
            }
            else if (Direction == "Deccending")
            {
                if (Altitude <= GroundAltitude & Altitude != 0)
                {
                    Echo("Reached Bottom!");
                    Direction = "Accending";
                    Running = false;
                }
                else
                {
                    switch (Stage)
                    {
                        case 1:
                            Echo("Activating Components...");
                            ApplyActionToAll(Welders, "OnOff_On");
                            ApplyActionToAll(Pistons, "OnOff_On");
                            SetPistonLimits(PistonMin, PistonMax);
                            Stage = 2;
                            break;
                        case 2:
                            Echo("Checking Top Rotors...");
                            if (CheckRotorsAttached(TopRotors))
                            {
                                Echo("Rotors Attached!");
                                Stage = 3;
                            }
                            else
                            {
                                Echo("Rotors not attached, checking Pistons...");
                                if (CheckPistonState("Retracted"))
                                {
                                    Echo("Pistons retracted, attempting to attach Top Rotors...");
                                    ApplyActionToAll(TopRotors, "Attach");
                                }
                                else
                                {
                                    Echo("Pistons not retracted, Retracting Pistons...");
                                    SetPistonVelocity(-PistonSpeed);
                                }
                            }
                            break;
                        case 3:
                            Echo("Detaching Bottom Rotors...");
                            ApplyActionToAll(BottomRotors, "Detach");

                            Echo("Checking Bottom Rotors...");
                            if (CheckRotorsDettached(BottomRotors))
                            {
                                Echo("Bottom Rotors Detached!");
                                Stage = 4;
                            }
                            break;
                        case 4:
                            Echo("Extending Pistons...");
                            SetPistonVelocity(PistonSpeed);

                            Echo("Checking if Pistons are Extended...");
                            if (CheckPistonState("Extended"))
                            {
                                Echo("Pistons Extended!");
                                Stage = 5;
                            }
                            break;
                        case 5:
                            Echo("Attaching Bottom Rotors...");
                            ApplyActionToAll(BottomRotors, "Attach");

                            Echo("Checking Bottom Rotors...");
                            if (CheckRotorsAttached(BottomRotors))
                            {
                                Echo("Bottom Rotors Attached!");
                                Stage = 6;
                            }
                            break;
                        case 6:
                            Echo("Detaching Top Rotors...");
                            ApplyActionToAll(TopRotors, "Detach");

                            Echo("Checking Top Rotors...");
                            if (CheckRotorsDettached(TopRotors))
                            {
                                Echo("Top Rotors Detached!");
                                Stage = 7;
                            }
                            break;
                        case 7:
                            Echo("Restaging Rear...");
                            SetPistonVelocity(-PistonSpeed);

                            Echo("Checking if Pistons are Retracted...");
                            if (CheckPistonState("Retracted"))
                            {
                                Echo("Pistons Retracted!");
                                Stage = 8;
                            }
                            break;
                        case 8:
                            Echo("Attaching Top Rotors...");
                            ApplyActionToAll(TopRotors, "Attach");

                            Echo("Checking Top Rotors...");
                            if (CheckRotorsAttached(TopRotors))
                            {
                                Echo("Top Rotors Attached!");
                                Stage = 1;
                            }
                            break;
                    }
                }
            }
        }

        //UPDATE FREQUENCY
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string args)
        {
            GetBlocks();
            GetCustomData();
            CheckSpaceReached();
            GetCurrentAltitude();
            ConsolePrint();
            ArgInput(args);

            if (Running)
            {
                switch (Mode)
                {
                    case "Reset":
                        Reset();
                        break;
                    case "Construction":
                        Construct();
                        break;
                    case "Repair":
                        Repair();
                        break;
                }
            }
            else
            {
                Echo("System Idle");
            }

            SetCustomData();
        }

        #endregion // Needle_V4
    }
}