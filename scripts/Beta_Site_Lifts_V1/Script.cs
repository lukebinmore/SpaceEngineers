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
namespace Beta_Site_Lifts_V1
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
        #region Beta_Site_Lifts_V1

        //GLOBALS
        Dictionary<string, string> Blocks = new Dictionary<string, string>() {
            {"Controller", "Beta Site - PB - Lifts"},
            {"Pistons", "Beta Site - Piston - Lift"},
            {"Hinges1", "Beta Site - Hinge 1 - Lift"},
            {"Hinges2", "Beta Site - Hinge 2 - Lift"},
            {"Lights", "Beta Site - Light - Lift"}
        };

        //DATA
        float PistonSpeed;
        float HingeSpeed;

        IMyCubeGrid grid;
        IMyTerminalBlock Controller;

        public class LiftBlocks
        {
            public float PistonSpeed { get; set; }
            public float HingeSpeed { get; set; }
            public string Status { get; set; } = "idle";
            public string Position { get; set; }
            public string Target { get; set; } = "none";
            public IMyPistonBase Piston { get; set; }
            public IMyMotorAdvancedStator Hinge1 { get; set; }
            public IMyMotorAdvancedStator Hinge2 { get; set; }
            public IMyTerminalBlock Light { get; set; }

            public void SetPistonSpeed(float Speed)
            {
                Piston.SetValue<float>("Velocity", Speed);
            }

            public string HingeStatus()
            {
                int Hinge1Angle = Convert.ToInt32(Math.Round(Hinge1.Angle * (180.0f / Math.PI), 3));
                int Hinge1Max = Convert.ToInt32(Hinge1.GetValue<float>("UpperLimit"));
                int Hinge1Min = Convert.ToInt32(Hinge1.GetValue<float>("LowerLimit"));
                int Hinge2Angle = Convert.ToInt32(Math.Round(Hinge2.Angle * (180.0f / Math.PI), 3));
                int Hinge2Min = Convert.ToInt32(Hinge2.GetValue<float>("LowerLimit"));
                int Hinge2Max = Convert.ToInt32(Hinge2.GetValue<float>("UpperLimit"));

                if (Hinge1Angle == Hinge1Max && Hinge2Angle == Hinge2Max)
                {
                    return "up";
                }
                else if (Hinge1Angle == Hinge1Min && Hinge2Angle == Hinge2Min)
                {
                    return "down";
                }

                return "moving";
            }

            public void SetHingeLock(bool Lock)
            {
                Hinge1.RotorLock = Lock;
                Hinge2.RotorLock = Lock;
            }

            public void SetHingeSpeed(float Speed)
            {
                Hinge1.SetValue<float>("Velocity", Speed);
                Hinge2.SetValue<float>("Velocity", Speed);
            }

            public void SetLightToggle(bool Activate)
            {
                Light.ApplyAction(Activate ? "OnOff_On" : "OnOff_Off");
            }

            public void CheckPosition()
            {
                string HingeCurrStatus = HingeStatus();
                string PistonCurrStatus = Piston.Status.ToString();

                if (PistonCurrStatus == "Extended" && HingeCurrStatus == "down")
                {
                    Position = "down";
                    SetHingeLock(true);
                    SetLightToggle(false);
                }
                else if (PistonCurrStatus == "Retracted" && HingeCurrStatus == "up")
                {
                    Position = "up";
                    SetHingeLock(true);
                    SetLightToggle(false);
                }
                else
                {
                    Position = "moving";
                }
            }

            public bool CheckMovement()
            {
                if (Target == Position)
                {
                    Target = "none";
                    Status = "idle";
                    return false;
                }
                else
                {
                    return true;
                }
            }

            public void MoveLift()
            {
                string HingeCurrStatus = HingeStatus();
                string PistonCurrStatus = Piston.Status.ToString();

                if (Target == "up")
                {
                    if (Status == "stage 1")
                    {
                        SetLightToggle(true);
                        SetHingeLock(false);
                        SetHingeSpeed(HingeSpeed);
                        Status = HingeCurrStatus == "up" ? "stage 2" : "stage 1";
                    }
                    else if (Status == "stage 2")
                    {
                        SetHingeLock(true);
                        SetPistonSpeed(-PistonSpeed);
                    }
                }
                else if (Target == "down")
                {
                    if (Status == "stage 1")
                    {
                        SetLightToggle(true);
                        SetPistonSpeed(PistonSpeed);
                        Status = PistonCurrStatus == "Extended" ? "stage 2" : "stage 1";
                    }
                    else if (Status == "stage 2")
                    {
                        SetHingeLock(false);
                        SetHingeSpeed(-HingeSpeed);
                    }
                }
            }

            public void NewTarget(string Direction)
            {
                if (Status == "idle")
                {
                    Target = Direction;
                    Status = "stage 1";
                }
            }
        }

        Dictionary<string, LiftBlocks> Lifts = new Dictionary<string, LiftBlocks>();

        //METHODS
        public void GetBlocks()
        {
            grid = Me.CubeGrid as IMyCubeGrid;
            Controller = GridTerminalSystem.GetBlockWithName(Blocks["Controller"]);

            List<IMyTerminalBlock> Pistons = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(Blocks["Pistons"], Pistons);

            List<IMyTerminalBlock> Hinges1 = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(Blocks["Hinges1"], Hinges1);

            List<IMyTerminalBlock> Hinges2 = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(Blocks["Hinges2"], Hinges2);

            List<IMyTerminalBlock> Lights = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(Blocks["Lights"], Lights);

            foreach (IMyTerminalBlock Block in Pistons)
            {
                string[] Name = Block.CustomName.Trim().Split(' ');
                if (!Lifts.ContainsKey(Name.Last()))
                {
                    Lifts.Add(Name.Last(), new LiftBlocks() { Piston = Block as IMyPistonBase });
                }

                Lifts[Name.Last()].Piston = Block as IMyPistonBase;
            }

            foreach (IMyTerminalBlock Block in Hinges1)
            {
                string[] Name = Block.CustomName.Trim().Split(' ');
                if (!Lifts.ContainsKey(Name.Last()))
                {
                    Lifts.Add(Name.Last(), new LiftBlocks() { Hinge1 = Block as IMyMotorAdvancedStator });
                }

                Lifts[Name.Last()].Hinge1 = Block as IMyMotorAdvancedStator;
            }

            foreach (IMyTerminalBlock Block in Hinges2)
            {
                string[] Name = Block.CustomName.Trim().Split(' ');
                if (!Lifts.ContainsKey(Name.Last()))
                {
                    Lifts.Add(Name.Last(), new LiftBlocks() { Hinge2 = Block as IMyMotorAdvancedStator });
                }

                Lifts[Name.Last()].Hinge2 = Block as IMyMotorAdvancedStator;
            }

            foreach (IMyTerminalBlock Block in Lights)
            {
                string[] Name = Block.CustomName.Trim().Split(' ');
                if (!Lifts.ContainsKey(Name.Last()))
                {
                    Lifts.Add(Name.Last(), new LiftBlocks() { Light = Block });
                }

                Lifts[Name.Last()].Light = Block;
            }
        }

        public void GetCustomData()
        {
            string[] CustomData = Controller.CustomData.Trim().Split('\n');

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
                    else
                    {
                        data.Add(part[0].Trim().Replace(' ', '_'), part[1].Trim());
                    }
                }

                PistonSpeed = float.Parse(data["Piston_Speed"]);
                HingeSpeed = float.Parse(data["Hinge_Speed"]);
            }
        }

        public void SetCustomData()
        {
            string[] CustomData = Controller.CustomData.Trim().Split('\n');

            if (CustomData[0] == "")
            {
                Controller.CustomData = (
                    "#####SETTINGS#####\n" +
                    "Piston Speed: 2.5\n" +
                    "Hinge Speed: 2.5"
                );
            }
            else
            {
                Controller.CustomData = (
                    "#####SETTINGS#####\n" +
                    "Piston Speed: " + PistonSpeed + "\n" +
                    "Hinge Speed: " + HingeSpeed
                );
            }
        }

        public void ApplySettings()
        {
            foreach (var Lift in Lifts)
            {
                Lift.Value.PistonSpeed = PistonSpeed;
                Lift.Value.HingeSpeed = HingeSpeed;
            }
        }

        public void UpdateConsole()
        {
            Echo("#####SETTINGS#####");
            Echo("Piston Speed: " + PistonSpeed.ToString());
            Echo("Hinge Speed: " + HingeSpeed.ToString());
            Echo("##################\n");

            foreach (var Lift in Lifts)
            {
                Echo(" - Lift " + Lift.Key);
                Echo(" * Status: " + Lift.Value.Status.ToUpper());
                Echo(" * Poisition: " + Lift.Value.Position.ToUpper());
                Echo(" * Target: " + Lift.Value.Target.ToUpper());
                Echo("\n");
            }
        }

        public void UpdateLifts()
        {
            foreach (var Lift in Lifts)
            {
                Lift.Value.CheckPosition();

                if (Lift.Value.CheckMovement())
                {
                    Lift.Value.MoveLift();
                }
            }

            UpdateConsole();
        }

        public void ArgsInput(string input)
        {
            string[] args = input.ToLower().Trim().Split(' ');

            if (args.Length == 2)
            {
                if (args[0] == "up")
                {
                    Lifts[args[1]].NewTarget("up");
                }
                else if (args[0] == "down")
                {
                    Lifts[args[1]].NewTarget("down");
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
            ApplySettings();
            UpdateLifts();
            ArgsInput(args);
            SetCustomData();
        }

        #endregion // Beta_Site_Lifts_V1
    }
}