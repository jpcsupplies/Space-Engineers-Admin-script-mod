﻿namespace midspace.adminscripts
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;
    using VRage;
    using VRage.Game.ModAPI;
    using VRageMath;

    public class CommandStopAll : ChatCommand
    {
        public CommandStopAll()
            : base(ChatCommandSecurity.Admin, "stopall", new[] { "/stopall" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/stopall <range>", "Stops all motion of everything in the specified <range>. Range will default to 100m if not specified.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/stopall(?:\s{1,}(?<RANGE>[^\s]*)){0,1}", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var strRange = match.Groups["RANGE"].Captures.Count > 0 ? match.Groups["RANGE"].Captures[0].Value : "100";
                double range = 100;
                double.TryParse(strRange, out range);
                range = Math.Abs(range);

                Vector3D destination;
                if (MyAPIGateway.Session.CameraController is MySpectator)
                {
                    destination = MyAPIGateway.Session.Camera.WorldMatrix.Translation;
                }
                else
                {
                    var playerEntity = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity;
                    destination = playerEntity.WorldAABB.Center;
                }
                var sphere = new BoundingSphereD(destination, range);
                var entityList = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

                entityList = entityList.Where(e =>
                    (e is IMyFloatingObject)
                    || (e is IMyCharacter)
                    || (e is IMyCubeGrid)).ToList();

                int counter = 0;
                foreach (var item in entityList)
                {
                    // Check for null physics and IsPhantom, to prevent picking up primitives.
                    if (item.Physics != null && !item.Physics.IsPhantom)
                    {
                        if (MyAPIGateway.Multiplayer.MultiplayerActive)
                            MessageSyncEntity.Process(item, SyncEntityType.Stop);
                        else
                            item.Stop();
                        counter++;
                    }
                }

                MyAPIGateway.Utilities.ShowMessage("Stopped", "{0} items within {1:N}m.", counter, range);
                return true;
            }

            return false;
        }
    }
}
