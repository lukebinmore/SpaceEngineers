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

/*
 * Must be unique per each script project.
 * Prevents collisions of multiple `class Program` declarations.
 * Will be used to detect the ingame script region, whose name is the same.
 */
namespace Connection_Detector_V1
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
        #region Connection_Detector_V1

        string Antenna = "SBSC - LA";
        string Target = "SBSC - Antenna Dish";
        string ConnectedAction = "OnOff_Off";
        string DissconnectedAction = "OnOff_On";


        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            IMyLaserAntenna AntennaBlock = GridTerminalSystem.GetBlockWithName(Antenna) as IMyLaserAntenna;
            IMyTerminalBlock TargetBlock = GridTerminalSystem.GetBlockWithName(Target) as IMyTerminalBlock;

            if (AntennaBlock.Status.ToString() != "Connected")
            {
                TargetBlock.ApplyAction(DissconnectedAction);
                Echo("Antenna is connected!");
            }
            else
            {
                TargetBlock.ApplyAction(ConnectedAction);
                Echo("Antenna is dissconnected!");
            }
        }

        #endregion // Connection_Detector_V1
    }
}