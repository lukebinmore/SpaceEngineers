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
namespace Deep_Impact_Controller_V2
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
        #region Deep_Impact_Controller_V2

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        bool Running = false;
        bool CompsOn = false;
        bool Toggle = true;
        bool Finished = false;
        int Stage = 1;
        const float PistonDrillSpeed = 0.006f;
        const float PistonRetractSpeed = -0.05f;
        const float RotorSpeed = 0.2f;
        const string ShipName = "Deep Impact - ";
        const string PistonName = ShipName + "Piston";
        const string WelderName = ShipName + "Welder";
        const string ConnTopName = ShipName + "Connector - Top";
        const string ConnBottomName = ShipName + "Connector - Bottom";
        const string ProjectorName = ShipName + "Projector";
        const string DrillsName = ShipName + "Drill";
        const string LightsName = ShipName + "Light - Warning";
        const string ControllerName = ShipName + "PB - Controller";
        const string RotorName = ShipName + "Rotor";


        public void Main(string args)
        {

            List<IMyTerminalBlock> Pistons = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> Drills = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> Conns_Top = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> Conns_Bottom = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> Lights = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> Welders = new List<IMyTerminalBlock>();

            GridTerminalSystem.SearchBlocksOfName(PistonName, Pistons);
            GridTerminalSystem.SearchBlocksOfName(DrillsName, Drills);
            GridTerminalSystem.SearchBlocksOfName(ConnTopName, Conns_Top);
            GridTerminalSystem.SearchBlocksOfName(ConnBottomName, Conns_Bottom);
            GridTerminalSystem.SearchBlocksOfName(LightsName, Lights);
            GridTerminalSystem.SearchBlocksOfName(WelderName, Welders);

            IMyTerminalBlock Controller = (IMyTerminalBlock)GridTerminalSystem.GetBlockWithName(ControllerName);
            IMyProjector Projector = (IMyProjector)GridTerminalSystem.GetBlockWithName(ProjectorName);
            IMyMotorStator Rotor = (IMyMotorStator)GridTerminalSystem.GetBlockWithName(RotorName);

            string[] States = Controller.CustomData.Split(':');
            if (States[0] != "")
            {
                Running = Convert.ToBoolean(States[0]);
                Stage = Int32.Parse(States[1]);
                Toggle = Convert.ToBoolean(States[2]);
            }

            if (args == "Start")
            {
                Running = true;
            }
            else if (args == "Stop")
            {
                Running = false;
            }
            else if (args == "Toggle")
            {
                if (Toggle)
                {
                    Toggle = false;
                }
                else
                {
                    Toggle = true;
                }
            }

            if (Projector.RemainingBlocks == 0)
            {
                Finished = true;
            }
            else
            {
                Finished = false;
            }

            Echo("Running: " + Running.ToString());
            Echo("Loop: " + Toggle);

            if (Running)
            {
                if (!CompsOn)
                {
                    CompsOn = true;
                    Echo("Activating Components...");

                    Echo(" - Rotor");
                    Rotor.SetValue("Velocity", RotorSpeed);

                    Echo(" - Projector");
                    Projector.ApplyAction("OnOff_On");

                    Echo(" - Pistons");
                    for (int i = 0; i < Pistons.Count; i++)
                    {
                        Pistons[i].ApplyAction("OnOff_On");
                    }

                    Echo(" - Drills");
                    for (int i = 0; i < Drills.Count; i++)
                    {
                        Drills[i].ApplyAction("OnOff_On");
                    }

                    Echo(" - Welders");
                    for (int i = 0; i < Welders.Count; i++)
                    {
                        Welders[i].ApplyAction("OnOff_On");
                    }

                    Echo(" - Lights");
                    for (int i = 0; i < Lights.Count; i++)
                    {
                        Lights[i].ApplyAction("OnOff_On");
                    }
                }

                Echo("Stage: " + Stage.ToString());

                if (Stage == 1 && !Finished)
                {
                    Echo("Checking Connectors...");

                    bool Conns_Locked = true;

                    for (int i = 0; i < Conns_Top.Count; i++)
                    {
                        IMyShipConnector Curr_Conn = Conns_Top[i] as IMyShipConnector;
                        if (Curr_Conn.Status.ToString() != "Connected")
                        {
                            Conns_Locked = false;
                        }
                    }

                    for (int i = 0; i < Conns_Bottom.Count; i++)
                    {
                        IMyShipConnector Curr_Conn = Conns_Bottom[i] as IMyShipConnector;
                        if (Curr_Conn.Status.ToString() != "Connected")
                        {
                            Conns_Locked = false;
                        }
                    }

                    if (Conns_Locked)
                    {
                        Stage = 2;
                    }
                }
                else if (Stage == 2)
                {
                    Echo("Releasing Top Connectors...");

                    for (int i = 0; i < Conns_Top.Count; i++)
                    {
                        IMyShipConnector Curr_Conn = Conns_Top[i] as IMyShipConnector;
                        Curr_Conn.Disconnect();
                        Curr_Conn.ApplyAction("OnOff_Off");
                    }

                    Stage = 3;
                }
                else if (Stage == 3)
                {
                    Echo("Extending Pistons...");

                    for (int i = 0; i < Pistons.Count; i++)
                    {
                        Pistons[i].SetValue("Velocity", PistonDrillSpeed);
                    }

                    Stage = 4;
                }
                else if (Stage == 4)
                {
                    Echo("Checking Extend Complete...");

                    bool PistonsComplete = true;

                    for (int i = 0; i < Pistons.Count; i++)
                    {
                        IMyPistonBase Curr_Piston = Pistons[i] as IMyPistonBase;
                        if (Curr_Piston.Status.ToString() != "Extended")
                        {
                            PistonsComplete = false;
                        }
                    }

                    if (PistonsComplete)
                    {
                        Stage = 5;
                    }
                }
                else if (Stage == 5)
                {
                    Echo("Connecting Top Connectors...");

                    bool Conns_Locked = true;

                    for (int i = 0; i < Conns_Top.Count; i++)
                    {
                        IMyShipConnector Curr_Conn = Conns_Top[i] as IMyShipConnector;
                        Curr_Conn.ApplyAction("OnOff_On");
                        Curr_Conn.Connect();
                        if (Curr_Conn.Status.ToString() != "Connected")
                        {
                            Conns_Locked = false;
                        }
                    }

                    if (Conns_Locked)
                    {
                        Stage = 6;
                    }
                }
                else if (Stage == 6)
                {
                    Echo("Releasing Bottom Connectors...");

                    for (int i = 0; i < Conns_Bottom.Count; i++)
                    {
                        IMyShipConnector Curr_Conn = Conns_Bottom[i] as IMyShipConnector;
                        Curr_Conn.Disconnect();
                        Curr_Conn.ApplyAction("OnOff_Off");
                    }

                    Stage = 7;
                }
                else if (Stage == 7)
                {
                    Echo("Retracting Pistons...");

                    for (int i = 0; i < Pistons.Count; i++)
                    {
                        Pistons[i].SetValue("Velocity", -1f);
                    }

                    Stage = 8;
                }
                else if (Stage == 8)
                {
                    Echo("Checking Retract Complete...");

                    bool PistonsComplete = true;

                    for (int i = 0; i < Pistons.Count; i++)
                    {
                        IMyPistonBase Curr_Piston = Pistons[i] as IMyPistonBase;
                        if (Curr_Piston.Status.ToString() != "Retracted")
                        {
                            PistonsComplete = false;
                        }
                    }

                    if (PistonsComplete)
                    {
                        Stage = 9;
                    }
                }
                else if (Stage == 9)
                {
                    Echo("Connecting Bottom Connectors...");

                    bool Conns_Locked = true;

                    for (int i = 0; i < Conns_Bottom.Count; i++)
                    {
                        IMyShipConnector Curr_Conn = Conns_Bottom[i] as IMyShipConnector;
                        Curr_Conn.ApplyAction("OnOff_On");
                        Curr_Conn.Connect();
                        if (Curr_Conn.Status.ToString() != "Connected")
                        {
                            Conns_Locked = false;
                        }
                    }

                    if (Conns_Locked)
                    {
                        Stage = 1;

                        if (!Toggle || Finished)
                        {
                            Running = false;
                        }
                    }
                }
            }
            else if (!Running)
            {
                CompsOn = false;
                Echo("Deactivating Components...");

                Echo(" - Rotor");
                Rotor.SetValue("Velocity", 0f);

                Echo(" - Projector");
                Projector.ApplyAction("OnOff_Off");

                Echo(" - Pistons");
                for (int i = 0; i < Pistons.Count; i++)
                {
                    Pistons[i].ApplyAction("OnOff_Off");
                }

                Echo(" - Drills");
                for (int i = 0; i < Drills.Count; i++)
                {
                    Drills[i].ApplyAction("OnOff_Off");
                }

                Echo(" - Welders");
                for (int i = 0; i < Welders.Count; i++)
                {
                    Welders[i].ApplyAction("OnOff_Off");
                }

                Echo(" - Lights");
                for (int i = 0; i < Lights.Count; i++)
                {
                    Lights[i].ApplyAction("OnOff_Off");
                }
            }

            Controller.CustomData = Running.ToString().ToLower() + ':' + Stage.ToString() + ':' + Toggle.ToString().ToLower();
        }

        #endregion // Deep_Impact_Controller_V2
    }
}