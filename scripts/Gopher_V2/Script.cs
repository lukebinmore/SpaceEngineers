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
namespace Gopher_V2
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
        #region Gopher_V2

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        //GLOBALS
        bool Running = false;
        int Stage = 1;

        public void PauseScript(IMyTimerBlock Timer, float Delay, IMyTerminalBlock PB)
        {
            Timer.SetValueFloat("TriggerDelay", Delay);
            Timer.StartCountdown();
            PB.ApplyAction("OnOff_Off");
        }

        public void Main(string args)
        {
            //GROUPS
            List<IMyTerminalBlock> HeadPistons = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> RearPistons = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> Drills = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> Welders = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> Grinders = new List<IMyTerminalBlock>();

            GridTerminalSystem.SearchBlocksOfName("Gopher - Piston - Head", HeadPistons);
            GridTerminalSystem.SearchBlocksOfName("Gopher - Piston - Rear", RearPistons);
            GridTerminalSystem.SearchBlocksOfName("Gopher - Drill", Drills);
            GridTerminalSystem.SearchBlocksOfName("Gopher - Welder", Welders);
            GridTerminalSystem.SearchBlocksOfName("Gopher - Grinder", Grinders);

            //BLOCKS
            IMyShipMergeBlock MergeBlock = (IMyShipMergeBlock)GridTerminalSystem.GetBlockWithName("Gopher - Merge Block");
            IMyShipConnector Connector = (IMyShipConnector)GridTerminalSystem.GetBlockWithName("Gopher - Connector - Move");
            IMyTerminalBlock Controller = (IMyTerminalBlock)GridTerminalSystem.GetBlockWithName("Gopher - PB - Controller");
            IMyTimerBlock Pauser = (IMyTimerBlock)GridTerminalSystem.GetBlockWithName("Gopher - Timer Block");
            IMyTerminalBlock Projector = (IMyTerminalBlock)GridTerminalSystem.GetBlockWithName("Gopher - Projector");

            //SETTINGS
            const float PistonExtendSpeed = 0.1f;
            const float PistonRetractSpeed = -1f;

            ///////////////////////////////////////////////////////////////////////
            if (args == "Start")
            {
                Running = true;
            }
            else if (args == "Stop")
            {
                Running = false;
            }

            if (Stage == 1)
            {
                if (Running == true)
                {
                    if (MergeBlock.IsConnected && Connector.Status.ToString() == "Connected")
                    {
                        for (int i = 0; i < Drills.Count; i++)
                        {
                            Drills[i].ApplyAction("OnOff_On");
                        }
                        for (int i = 0; i < Welders.Count; i++)
                        {
                            Welders[i].ApplyAction("OnOff_On");
                        }
                        for (int i = 0; i < Grinders.Count; i++)
                        {
                            Grinders[i].ApplyAction("OnOff_On");
                        }
                        Connector.Disconnect();
                        for (int i = 0; i < HeadPistons.Count; i++)
                        {
                            HeadPistons[i].SetValue<float>("Velocity", PistonExtendSpeed);
                        }
                        for (int i = 0; i < RearPistons.Count; i++)
                        {
                            RearPistons[i].SetValue<float>("Velocity", -PistonExtendSpeed);
                        }

                        Projector.ApplyAction("OnOff_Off");
                        Projector.ApplyAction("OnOff_On");

                        Stage = 2;
                        Echo("Stage 1");

                        PauseScript(Pauser, 5, Controller);
                    }
                }
                else if (Running == false)
                {
                    for (int i = 0; i < Drills.Count; i++)
                    {
                        Drills[i].ApplyAction("OnOff_Off");
                    }
                    for (int i = 0; i < Welders.Count; i++)
                    {
                        Welders[i].ApplyAction("OnOff_Off");
                    }
                    for (int i = 0; i < Grinders.Count; i++)
                    {
                        Grinders[i].ApplyAction("OnOff_Off");
                    }
                }
            }
            else if (Stage == 2)
            {
                if (Connector.Status.ToString() == "Connectable")
                {
                    Stage = 3;
                    Echo("Stage 2");
                    PauseScript(Pauser, 3, Controller);
                }
            }
            else if (Stage == 3)
            {
                Connector.Connect();
                if (Connector.Status.ToString() == "Connected")
                {
                    MergeBlock.ApplyAction("OnOff_Off");
                    for (int i = 0; i < HeadPistons.Count; i++)
                    {
                        HeadPistons[i].SetValue<float>("Velocity", PistonRetractSpeed);
                    }
                    for (int i = 0; i < RearPistons.Count; i++)
                    {
                        RearPistons[i].SetValue<float>("Velocity", -PistonRetractSpeed);
                    }
                    Stage = 4;
                    Echo("Stage 3");
                    PauseScript(Pauser, 2, Controller);
                }
            }
            else if (Stage == 4)
            {
                MergeBlock.ApplyAction("OnOff_On");
                if (MergeBlock.IsConnected)
                {
                    Stage = 1;
                    Echo("Stage 4");
                    PauseScript(Pauser, 2, Controller);
                }
            }
        }

        #endregion // Gopher_V2
    }
}