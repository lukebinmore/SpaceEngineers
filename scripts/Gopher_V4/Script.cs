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
namespace Gopher_V4
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
        #region Gopher_V4

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        //GLOBALS
        string DefaultSettings = "Running=false\nStage=1\nWelder Setting=5\n";

        public void Main(string args)
        {
            //GROUPS
            List<IMyTerminalBlock> Pistons = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> Drills = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> Welders = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> Grinders = new List<IMyTerminalBlock>();

            GridTerminalSystem.SearchBlocksOfName("Gopher - Piston", Pistons);
            GridTerminalSystem.SearchBlocksOfName("Gopher - Drill", Drills);
            GridTerminalSystem.SearchBlocksOfName("Gopher - Welder", Welders);
            GridTerminalSystem.SearchBlocksOfName("Gopher - Grinder", Grinders);

            //BLOCKS
            IMyTerminalBlock Controller = (IMyTerminalBlock)GridTerminalSystem.GetBlockWithName("Gopher - PB - Controller");
            IMyTerminalBlock RearProjector = (IMyTerminalBlock)GridTerminalSystem.GetBlockWithName("Gopher - Projector - Rear");
            IMyTerminalBlock FrontProjector = (IMyTerminalBlock)GridTerminalSystem.GetBlockWithName("Gopher - Projector - Front");
            IMyMotorStator RearRotor = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("Gopher - Rotor - Rear");
            IMyMotorStator FrontRotor = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("Gopher - Rotor - Front");

            IMyCubeGrid grid = Controller.CubeGrid as IMyCubeGrid;
            Dictionary<string, string> Settings = new Dictionary<string, string>();
            float PistonExtendSpeed = 0.05f;
            float PistonRetractSpeed = 0f;

            ///////////////////////////////////////////////////////////////////////
            string[] CustomData = Controller.CustomData.Split('\n');
            if (CustomData[0] == "")
            {
                Controller.CustomData = DefaultSettings;
            }
            foreach (string line in CustomData)
            {
                string[] array = line.Split('=');
                if (array.Count() > 1)
                {
                    Settings.Add(array[0], array[1].ToLower());
                }
            }

            switch (Settings["Welder Setting"])
            {
                case "0.5":
                    PistonRetractSpeed = -0.05f;
                    break;
                case "1":
                    PistonRetractSpeed = -0.1f;
                    break;
                case "2":
                    PistonRetractSpeed = -0.2f;
                    break;
                case "5":
                    PistonRetractSpeed = -0.4f;
                    break;
            }

            if (args.ToLower() == "start")
            {
                Settings["Running"] = "true";
                foreach (IMyTerminalBlock welder in Welders)
                {
                    welder.ApplyAction("OnOff_On");
                }
                foreach (IMyTerminalBlock grinder in Grinders)
                {
                    grinder.ApplyAction("OnOff_On");
                }
                foreach (IMyTerminalBlock drill in Drills)
                {
                    drill.ApplyAction("OnOff_On");
                }
            }
            else if (args.ToLower() == "stop")
            {
                Settings["Running"] = "false";
                foreach (IMyTerminalBlock welder in Welders)
                {
                    welder.ApplyAction("OnOff_Off");
                }
                foreach (IMyTerminalBlock grinder in Grinders)
                {
                    grinder.ApplyAction("OnOff_Off");
                }
                foreach (IMyTerminalBlock drill in Drills)
                {
                    drill.ApplyAction("OnOff_Off");
                }
            }

            Echo("Running: " + Settings["Running"]);
            Echo("Stage: " + Settings["Stage"]);
            Echo("\n");
            Echo("Settings:");
            Echo("Welder Setting: " + Settings["Welder Setting"]);

            if (Convert.ToBoolean(Settings["Running"]))
            {
                if (Settings["Stage"] == "1")
                {
                    RearProjector.ApplyAction("OnOff_On");
                    FrontRotor.ApplyAction("Detach");
                    foreach (IMyTerminalBlock piston in Pistons)
                    {
                        piston.SetValue<float>("Velocity", PistonExtendSpeed);
                    }

                    Settings["Stage"] = "2";
                }
                else if (Settings["Stage"] == "2")
                {
                    bool PistonExtended = true;

                    foreach (IMyTerminalBlock piston in Pistons)
                    {
                        var pistonTemp = piston as IMyPistonBase;
                        if (pistonTemp.Status.ToString() != "Extended")
                        {
                            PistonExtended = false;
                        }
                    }

                    if (PistonExtended)
                    {
                        FrontRotor.ApplyAction("Attach");
                    }

                    if (FrontRotor.IsAttached)
                    {
                        Settings["Stage"] = "3";
                    }
                }
                else if (Settings["Stage"] == "3")
                {
                    if (FrontRotor.IsAttached)
                    {
                        RearProjector.ApplyAction("OnOff_Off");
                        FrontProjector.ApplyAction("OnOff_On");
                        RearRotor.ApplyAction("Detach");

                        foreach (IMyTerminalBlock piston in Pistons)
                        {
                            piston.SetValue<float>("Velocity", PistonRetractSpeed);
                        }

                        Settings["Stage"] = "4";
                    }
                }
                else if (Settings["Stage"] == "4")
                {
                    bool PistonRetracted = true;

                    foreach (IMyTerminalBlock piston in Pistons)
                    {
                        var pistonTemp = piston as IMyPistonBase;
                        if (pistonTemp.Status.ToString() != "Retracted")
                        {
                            PistonRetracted = false;
                        }
                    }

                    if (PistonRetracted)
                    {
                        RearRotor.ApplyAction("Attach");
                    }

                    if (RearRotor.IsAttached)
                    {
                        Settings["Stage"] = "5";
                    }
                }
                else if (Settings["Stage"] == "5")
                {
                    if (RearRotor.IsAttached)
                    {
                        RearProjector.ApplyAction("OnOff_On");
                        FrontProjector.ApplyAction("OnOff_Off");

                        Settings["Stage"] = "1";
                    }
                }
            }


            string updatedCustomData = "";
            foreach (string key in Settings.Keys)
            {
                updatedCustomData += key + "=" + Settings[key] + "\n";
            }
            Controller.CustomData = updatedCustomData;
        }

        #endregion // Gopher_V4
    }
}