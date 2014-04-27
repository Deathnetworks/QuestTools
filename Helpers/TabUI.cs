using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Zeta.Bot;
using Zeta.Bot.Dungeons;
using Zeta.Bot.Logic;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.Actors.Gizmos;

namespace QuestTools
{
    class TabUI
    {
        private static Button btnDumpActors, btnOpenLogFile, btnResetGrid;
        private static Button btnSafeMoveTo, btnMoveToActor, btnMoveToMapMarker, btnIfTag, btnWaitTimerTag, btnExploreAreaTag, btnUseWaypointTag;

        private static string Indent3Hang = "                       ";

        internal static void InstallTab()
        {
            Application.Current.Dispatcher.Invoke(
                new System.Action(
                    () =>
                    {
                        // 1st column x: 432
                        // 2nd column x: 552
                        // 3rd column x: 672

                        // Y rows: 10, 33, 56, 79, 102

                        btnDumpActors = new Button
                        {
                            Width = 120,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                            Content = "Dump Actor Attribs"
                        };

                        btnOpenLogFile = new Button
                        {
                            Width = 120,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                            Content = "Open Log File"
                        };

                        btnResetGrid = new Button
                        {
                            Width = 120,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                            Content = "Force Reset Grid"
                        };

                        btnSafeMoveTo = new Button
                        {
                            Width = 120,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                            Content = "SafeMoveTo"
                        };

                        btnMoveToActor = new Button
                        {
                            Width = 120,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                            Content = "MoveToActor"
                        };

                        btnMoveToMapMarker = new Button
                        {
                            Width = 120,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                            Content = "MoveToMapMarker"
                        };

                        btnIfTag = new Button
                        {
                            Width = 120,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                            Content = "IfTag"
                        };

                        btnWaitTimerTag = new Button
                        {
                            Width = 120,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                            Content = "WaitTag"
                        };

                        btnExploreAreaTag = new Button
                        {
                            Width = 120,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                            Content = "ExploreTag"
                        };

                        btnUseWaypointTag = new Button
                        {
                            Width = 120,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                            Content = "UseWaypoint"
                        };


                        Window mainWindow = Application.Current.MainWindow;

                        btnDumpActors.Click += new RoutedEventHandler(btnDumpActors_Click);
                        btnOpenLogFile.Click += new RoutedEventHandler(btnOpenLogFile_Click);
                        btnResetGrid.Click += new RoutedEventHandler(btnResetGrid_Click);

                        btnSafeMoveTo.Click += btnSafeMoveTo_Click;
                        btnMoveToActor.Click += btnMoveToActor_Click;
                        btnMoveToMapMarker.Click += btnMoveToMapMarker_Click;
                        btnIfTag.Click += btnIfTag_Click;
                        btnExploreAreaTag.Click += btnExploreAreaTag_Click;
                        btnWaitTimerTag.Click += btnWaitTimerTag_Click;
                        btnUseWaypointTag.Click += btnUseWaypointTag_Click;

                        UniformGrid uniformGrid = new UniformGrid()
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Top,
                            MaxHeight = 180,
                        };

                        uniformGrid.Children.Add(btnDumpActors);
                        uniformGrid.Children.Add(btnOpenLogFile);
                        uniformGrid.Children.Add(btnResetGrid);

                        uniformGrid.Children.Add(btnSafeMoveTo);
                        uniformGrid.Children.Add(btnMoveToActor);
                        uniformGrid.Children.Add(btnMoveToMapMarker);
                        uniformGrid.Children.Add(btnIfTag);
                        uniformGrid.Children.Add(btnExploreAreaTag);
                        uniformGrid.Children.Add(btnWaitTimerTag);
                        uniformGrid.Children.Add(btnUseWaypointTag);


                        tabItem = new TabItem()
                        {
                            Header = "QuestTools",
                            ToolTip = "Profile Creation Tools",
                        };

                        tabItem.Content = uniformGrid;

                        var tabs = mainWindow.FindName("tabControlMain") as TabControl;
                        if (tabs == null)
                            return;

                        tabs.Items.Add(tabItem);
                    }
                )
            );
        }

        static void btnUseWaypointTag_Click(object sender, RoutedEventArgs e)
        {
            if (Zeta.Bot.BotMain.IsRunning)
            {
                BotMain.Stop();
            }
            string tagText = "";
            Thread.Sleep(500);
            try
            {
                using (var helper = new ZetaCacheHelper())
                {
                    if (!ZetaDia.IsInGame)
                        return;
                    if (ZetaDia.Me == null)
                        return;
                    if (!ZetaDia.Me.IsValid)
                        return;

                    ZetaDia.Actors.Update();

                    List<GizmoWaypoint> objList = (from o in ZetaDia.Actors.GetActorsOfType<GizmoWaypoint>(true, false)
                                                   where o.IsValid
                                                   orderby o.Position.Distance(ZetaDia.Me.Position)
                                                   select o).ToList();

                    string portalInfo = string.Empty;

                    if (objList.Any())
                    {
                        GizmoWaypoint obj = objList.FirstOrDefault();

                        tagText = string.Format("\n<UseWaypoint questId=\"{0}\" stepId=\"{1}\" waypointNumber=\"{2}\" name=\"{3}\" statusText=\"\" /> \n",
                            ZetaDia.CurrentQuest.QuestSNO, ZetaDia.CurrentQuest.StepId, obj.WaypointNumber, obj.Name);
                    }
                    else
                    {
                        tagText = string.Format("\n<UseWaypoint questId=\"{0}\" stepId=\"{1}\" waypointNumber=\"\" name=\"\" statusText=\"\" /> \n",
                            ZetaDia.CurrentQuest.QuestSNO, ZetaDia.CurrentQuest.StepId);
                    }
                    Clipboard.SetText(tagText);
                    Logger.Log(tagText);

                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
        }

        private static TabItem tabItem;

        internal static void RemoveTab()
        {
            Application.Current.Dispatcher.Invoke(
                new System.Action(
                    () =>
                    {
                        Window mainWindow = Application.Current.MainWindow;
                        var tabs = mainWindow.FindName("tabControlMain") as TabControl;
                        if (tabs == null)
                            return;
                        tabs.Items.Remove(tabItem);

                    }
                )
            );
        }

        private static void btnWaitTimerTag_Click(object sender, RoutedEventArgs e)
        {
            if (Zeta.Bot.BotMain.IsRunning)
            {
                BotMain.Stop();
            }
            Thread.Sleep(500);
            try
            {
                using (var helper = new ZetaCacheHelper())
                {
                    if (!ZetaDia.IsInGame)
                        return;
                    if (ZetaDia.Me == null)
                        return;
                    if (!ZetaDia.Me.IsValid)
                        return; ZetaDia.Actors.Update();
                    string levelAreaName = ZetaDia.SNO.LookupSNOName(SNOGroup.LevelArea, QuestTools.LevelAreaId);
                    string worldName = ZetaDia.WorldInfo.Name;

                    string tagText = string.Format("\n<WaitTimer questId=\"{3}\" stepId=\"{4}\" waitTime=\"500\" />\n",
                        ZetaDia.CurrentQuest.Name, worldName, levelAreaName, ZetaDia.CurrentQuest.QuestSNO, ZetaDia.CurrentQuest.StepId, ZetaDia.CurrentWorldId, QuestTools.LevelAreaId);
                    Clipboard.SetText(tagText);
                    Logger.Log(tagText);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }

        }

        private static void btnExploreAreaTag_Click(object sender, RoutedEventArgs e)
        {
            if (Zeta.Bot.BotMain.IsRunning)
            {
                BotMain.Stop();
            }

            try
            {
                using (var helper = new ZetaCacheHelper())
                {
                    if (!ZetaDia.IsInGame)
                        return;
                    if (ZetaDia.Me == null)
                        return;
                    if (!ZetaDia.Me.IsValid)
                        return;

                    ZetaDia.Actors.Update();

                    string questInfo;
                    string questHeader;
                    GetQuestInfoText(out questInfo, out questHeader);

                    string tagText =
"\n" + questHeader +
"\n<TrinityExploreDungeon " + questInfo + " until=\"ExitFound\" exitNameHash=\"0\" actorId=\"0\" pathPrecision=\"25\" boxSize=\"25\" boxTolerance=\"0.01\" objectDistance=\"45\">" +
"\n    <AlternateActors>" +
"\n        <AlternateActor actorId=\"0\" objectDistance=\"45\" />" +
"\n    </AlternateActors>" +
"\n    <PriorityScenes>" +
"\n        <PriorityScene sceneName=\"Exit\" />" +
"\n    </PriorityScenes>" +
"\n    <IgnoreScenes>" +
"\n        <IgnoreScene sceneName=\"_N_\" />" +
"\n        <IgnoreScene sceneName=\"_S_\" />" +
"\n        <IgnoreScene sceneName=\"_E_\" />" +
"\n        <IgnoreScene sceneName=\"_W_\" />" +
"\n    </IgnoreScenes>" +
"\n</TrinityExploreDungeon>";
                    Clipboard.SetText(tagText);
                    Logger.Log(tagText);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
        }

        private static void GetQuestInfoText(out string questInfo, out string questHeader)
        {
            string levelAreaName = ZetaDia.SNO.LookupSNOName(SNOGroup.LevelArea, QuestTools.LevelAreaId);
            string worldName = ZetaDia.WorldInfo.Name;


            if (ZetaDia.CurrentAct == Act.OpenWorld && ZetaDia.ActInfo.ActiveBounty != null)
            {
                questInfo = string.Format("questId=\"{0}\"", ZetaDia.ActInfo.ActiveBounty.Info.QuestSNO);
                questHeader = string.Format("\n<!-- Quest: {0} ({1}) World: {2} ({3}) LevelArea: {4} ({5}) -->",
                    ZetaDia.ActInfo.ActiveBounty.Info.Quest,
                    ZetaDia.ActInfo.ActiveBounty.Info.QuestSNO,
                    worldName,
                    ZetaDia.CurrentWorldId,
                    levelAreaName,
                    ZetaDia.CurrentLevelAreaId);
            }
            else
            {
                questInfo = string.Format("questId=\"{0}\" stepId=\"{1}\"", ZetaDia.CurrentQuest.QuestSNO, ZetaDia.CurrentQuest.StepId);
                questHeader = string.Format("\n<!-- Quest: {0} ({1}) World: {2} ({3}) LevelArea: {4} ({5}) -->",
                   ZetaDia.CurrentQuest.Name,
                   ZetaDia.CurrentQuest.QuestSNO,
                   worldName,
                   ZetaDia.CurrentWorldId,
                   levelAreaName,
                   ZetaDia.CurrentLevelAreaId);
            }
        }

        private static void btnIfTag_Click(object sender, RoutedEventArgs e)
        {
            if (Zeta.Bot.BotMain.IsRunning)
            {
                BotMain.Stop();
            }

            Thread.Sleep(500);
            try
            {
                using (var helper = new ZetaCacheHelper())
                {
                    if (!ZetaDia.IsInGame)
                        return;
                    if (ZetaDia.Me == null)
                        return;
                    if (!ZetaDia.Me.IsValid)
                        return;
                    ZetaDia.Actors.Update();
                    string levelAreaName = ZetaDia.SNO.LookupSNOName(SNOGroup.LevelArea, QuestTools.LevelAreaId);
                    string worldName = ZetaDia.WorldInfo.Name;

                    string tagText;
                    string questInfo;
                    string questHeader;

                    GetQuestInfoText(out questInfo, out questHeader);

                    if (ZetaDia.CurrentAct == Act.OpenWorld && ZetaDia.ActInfo.ActiveBounty != null)
                    {
                        tagText = string.Format(questHeader + "\n<If condition=\"HasQuest({5}) and CurrentWorldId=={6} and CurrentLevelAreaId=={7}\">\n\n</If>",
                             ZetaDia.ActInfo.ActiveBounty.Info.Quest, worldName, ZetaDia.CurrentWorldId, levelAreaName, ZetaDia.CurrentLevelAreaId, ZetaDia.ActInfo.ActiveBounty.Info.QuestSNO, ZetaDia.CurrentWorldId, QuestTools.LevelAreaId);
                    }
                    else if (ZetaDia.CurrentAct == Act.OpenWorld && ZetaDia.IsInTown)
                    {
                        tagText = string.Format(questHeader + "\n<If condition=\"HasQuest(0) and CurrentWorldId=={6} and CurrentLevelAreaId=={7}\">\n\n</If>",
                            ZetaDia.ActInfo.ActiveBounty.Info.Quest, worldName, ZetaDia.CurrentWorldId, levelAreaName, ZetaDia.CurrentLevelAreaId, ZetaDia.ActInfo.ActiveBounty.Info.QuestSNO, ZetaDia.CurrentWorldId, QuestTools.LevelAreaId);
                    }
                    else
                    {
                        tagText = string.Format(questHeader + "\n<If condition=\"IsActiveQuest({3}) and IsActiveQuestStep({4}) and CurrentWorldId=={5} and CurrentLevelAreaId=={6}\">\n\n</If>",
                            ZetaDia.CurrentQuest.Name, worldName, levelAreaName, ZetaDia.CurrentQuest.QuestSNO, ZetaDia.CurrentQuest.StepId, ZetaDia.CurrentWorldId, QuestTools.LevelAreaId);

                    }
                    Logger.Log(tagText);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
        }

        private static void btnMoveToMapMarker_Click(object sender, RoutedEventArgs e)
        {
            if (Zeta.Bot.BotMain.IsRunning)
            {
                BotMain.Stop();
            }

            Thread.Sleep(500);
            try
            {
                using (var helper = new ZetaCacheHelper())
                {
                    if (!ZetaDia.IsInGame)
                        return;
                    if (ZetaDia.Me == null)
                        return;
                    if (!ZetaDia.Me.IsValid)
                        return;

                    ZetaDia.Actors.Update();

                    MinimapMarker marker = ZetaDia.Minimap.Markers.CurrentWorldMarkers.OrderBy(m => m.Position.Distance2D(ZetaDia.Me.Position)).FirstOrDefault();

                    DiaObject portal = (from o in ZetaDia.Actors.GetActorsOfType<GizmoPortal>(true, false)
                                        orderby o.Position.Distance(ZetaDia.Me.Position)
                                        select o).FirstOrDefault();
                    if (portal == null)
                        portal = (from o in ZetaDia.Actors.GetActorsOfType<DiaObject>(true, false)
                                  orderby o.Position.Distance(marker.Position)
                                  select o).FirstOrDefault();



                    if (marker != null)
                    {
                        string questInfo;
                        string questHeader;
                        GetQuestInfoText(out questInfo, out questHeader);

                        string locationInfo = "";

                        if (!ZetaDia.WorldInfo.IsGenerated)
                        {
                            locationInfo = string.Format("x=\"{0:0}\" y=\"{1:0}\" z=\"{2:0}\" ",
                                portal.Position.X, portal.Position.Y, portal.Position.Z);
                        }

                        string tagText = string.Format(questHeader + "\n<MoveToMapMarker " + questInfo + " " + locationInfo + "markerNameHash=\"{0}\" actorId=\"{1}\" interactRange=\"{2}\" \n" +
                                    Indent3Hang + "pathPrecision=\"5\" pathPointLimit=\"250\" isPortal=\"True\" destinationWorldId=\"-1\" statusText=\"\" /> \n",
                                        marker.NameHash, portal.ActorSNO, portal.CollisionSphere.Radius);
                        Clipboard.SetText(tagText);
                        Logger.Log(tagText);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
        }

        private static void btnMoveToActor_Click(object sender, RoutedEventArgs e)
        {
            if (Zeta.Bot.BotMain.IsRunning)
            {
                BotMain.Stop();
            }
            string tagText = "";
            Thread.Sleep(500);
            try
            {
                using (var helper = new ZetaCacheHelper())
                {
                    if (!ZetaDia.IsInGame)
                        return;
                    if (ZetaDia.Me == null)
                        return;
                    if (!ZetaDia.Me.IsValid)
                        return;

                    ZetaDia.Actors.Update();

                    List<DiaObject> objList = (from o in ZetaDia.Actors.GetActorsOfType<DiaObject>(true, false)
                                               where (o is DiaGizmo || o is DiaUnit) &&
                                               !o.Name.StartsWith("Generic_Proxy") &&
                                               !o.Name.StartsWith("Start_Location") &&
                                               !(o is DiaPlayer)
                                               orderby o.Position.Distance(ZetaDia.Me.Position)
                                               select o).ToList();

                    string portalInfo = string.Empty;

                    string questInfo;
                    string questHeader;
                    GetQuestInfoText(out questInfo, out questHeader);

                    string locationInfo = "";

                    if (objList.Any())
                    {
                        DiaObject obj = objList.FirstOrDefault();

                        if (!ZetaDia.WorldInfo.IsGenerated)
                        {
                            locationInfo = string.Format(" x=\"{0:0}\" y=\"{1:0}\" z=\"{2:0}\" ",
                                obj.Position.X, obj.Position.Y, obj.Position.Z);
                        }

                        if (obj is GizmoPortal)
                            portalInfo = " isPortal=\"True\" destinationWorldId=\"-1\"";


                        tagText = string.Format(questHeader + "\n<MoveToActor " + questInfo + locationInfo + " actorId=\"{0}\" interactRange=\"{1:0}\" name=\"{2}\" " + portalInfo + " pathPrecision=\"5\" pathPointLimit=\"250\" statusText=\"\" /> \n",
                            obj.ActorSNO, obj.CollisionSphere.Radius + 1f, obj.Name);
                    }
                    else
                    {
                        tagText = string.Format(questHeader + "\n<MoveToActor " + questInfo + " x=\"\" y=\"\" z=\"\" actorId=\"\" interactRange=\"20\" pathPrecision=\"50\" pathPointLimit=\"250\" statusText=\"\" /> \n");
                    }
                    Clipboard.SetText(tagText);
                    Logger.Log(tagText);

                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
        }

        private static void btnSafeMoveTo_Click(object sender, RoutedEventArgs e)
        {
            if (Zeta.Bot.BotMain.IsRunning)
            {
                BotMain.Stop();
            }

            Thread.Sleep(500);
            try
            {
                using (var helper = new ZetaCacheHelper())
                {
                    if (!ZetaDia.IsInGame)
                        return;
                    if (ZetaDia.Me == null)
                        return;
                    if (!ZetaDia.Me.IsValid)
                        return;

                    ZetaDia.Actors.Update();

                    string tagText = string.Format("\n<SafeMoveTo questId=\"{3}\" stepId=\"{4}\" x=\"{0:0}\" y=\"{1:0}\" z=\"{2:0}\" pathPrecision=\"5\" pathPointLimit=\"250\" statusText=\"\" /> \n",
                        ZetaDia.Me.Position.X, ZetaDia.Me.Position.Y, ZetaDia.Me.Position.Z, ZetaDia.CurrentQuest.QuestSNO, ZetaDia.CurrentQuest.StepId);
                    Clipboard.SetText(tagText);
                    Logger.Log(tagText);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
        }

        private static void btnResetGrid_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (BotMain.IsRunning)
                {
                    BotMain.Stop();
                }
                if (!ZetaDia.IsInGame || !ZetaDia.Me.IsValid)
                    return;

                System.Threading.Thread.Sleep(500);

                GridSegmentation.Reset();
                GridSegmentation.Update();
                BrainBehavior.DungeonExplorer.Reset();
                BrainBehavior.DungeonExplorer.GetBestRoute();
            }
            catch (Exception ex)
            {
                Logger.Log("Could not reset grid: {0}", ex);
            }

        }

        private static void btnOpenLogFile_Click(object sender, RoutedEventArgs e)
        {
            string exePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            int myPid = Process.GetCurrentProcess().Id;
            DateTime startTime = Process.GetCurrentProcess().StartTime;
            string logFile = Path.Combine(exePath, "Logs", myPid + " " + startTime.ToString("yyyy-MM-dd HH.mm") + ".txt");

            Process.Start(logFile);
        }

        private static void btnDumpActors_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (ZetaDia.Memory.SaveCacheState())
                {
                    ZetaDia.Memory.DisableCache();

                    if (BotMain.IsRunning)
                    {
                        BotMain.Stop();
                        Thread.Sleep(1500);
                    }

                    double iType = -1;

                    
                    ZetaDia.Actors.Update();
                    var units = ZetaDia.Actors.GetActorsOfType<DiaUnit>(false, false)
                        .Where(o => o.IsValid)
                        .OrderBy(o => o.Distance);

                    iType = DumpUnits(units, iType);


                    //ZetaDia.Actors.Update();
                    var objects = ZetaDia.Actors.GetActorsOfType<DiaObject>(false, false)
                        .Where(o => o.IsValid)
                        .OrderBy(o => o.Distance);

                    iType = DumpObjects(objects, iType);

                    //ZetaDia.Actors.Update();
                    var gizmos = ZetaDia.Actors.GetActorsOfType<DiaGizmo>(true, false)
                        .Where(o => o.IsValid)
                        .OrderBy(o => o.Distance);

                    iType = DumpGizmos(gizmos, iType);

                    var items = ZetaDia.Actors.GetActorsOfType<DiaItem>(true, false)
                        .Where(o => o.IsValid)
                        .OrderBy(o => o.Distance);

                    iType = DumpItems(items, iType);

                    ZetaDia.Actors.Update();
                    var players = ZetaDia.Actors.GetActorsOfType<DiaPlayer>(true, false)
                        .Where(o => o.IsValid)
                        .OrderBy(o => o.Distance);

                    iType = DumpPlayers(players, iType);

                    //DumpService();

                    Logger.Log("ZetaDia.Service.Hero.IsValid={0}", ZetaDia.Service.Hero.IsValid);

                    //DumpPlayerProperties();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }

        }

        private static void DumpPlayerProperties()
        {
            Logger.Log("DiaActivePlayer properties:");

            Type type = typeof(DiaActivePlayer);
            foreach (PropertyInfo prop in type.GetProperties())
            {
                if (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string))
                {
                    Logger.Log("\nName: {0} Value: {1} Type: {2}", prop.Name, prop.GetValue(ZetaDia.Me, null), prop.PropertyType.Name);
                }
            }
        }

        private static double DumpUnits(IEnumerable<DiaUnit> units, double iType)
        {
            Logger.Debug("Units found: {0}", units.Count());
            foreach (DiaUnit o in units)
            {
                if (!o.IsValid)
                    continue;

                string attributesFound = "", propertiesFound = "";

                foreach (ActorAttributeType aType in Enum.GetValues(typeof(ActorAttributeType)))
                {
                    if (IgnoreActorAttributeTypes.Contains(aType))
                        continue;
                    iType = GetAttribute(iType, o, aType);
                    if (iType > 0)
                    {
                        attributesFound += aType.ToString() + "=" + iType.ToString() + ", ";
                    }
                }

                propertiesFound = ReadProperties(o, null);

                try
                {
                    Logger.Log("\nUnit ActorSNO: {0} Name: {1} Type: {2} Radius: {7:0.00} Position: {3} ({4}) Animation: {5} has Attributes:\n{6}\nProperties:\n{8}\n\n",
                                        o.ActorSNO, o.Name, o.ActorInfo.GizmoType, QuestTools.GetProfilePosition(o.Position),
                                        QuestTools.GetSimplePosition(o.Position),
                                        o.CommonData.CurrentAnimation, attributesFound, o.CollisionSphere.Radius, propertiesFound);
                }
                catch { }

            }
            return iType;
        }

        private static string ReadProperties<T>(T obj, HashSet<Tuple<Type, string>> checkedTypes)
        {
            if (obj == null)
                return "";

            string propertiesFound = "";
            if (checkedTypes == null)
                checkedTypes = new HashSet<Tuple<Type, string>>();
            foreach (var property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Tuple<Type, string> typeTuple = new Tuple<Type, string>(property.PropertyType, property.Name);

                if (property.PropertyType.IsValueType)
                {
                    try
                    {
                        string val = property.GetValue(obj, null).ToString().Replace("{", "<").Replace("}", ">");
                        propertiesFound += property.Name + ": " + val + " ";
                    }
                    catch (Exception ex)
                    {
                        propertiesFound += string.Format("\nError reading {0}: {1}", property.Name, ex.Message);
                    }
                }
                else if (property.PropertyType.IsClass && !checkedTypes.Contains(typeTuple))
                {
                    checkedTypes.Add(typeTuple);
                    try
                    {
                        if (!property.PropertyType.IsArray)
                        {
                            propertiesFound += "\n" + property.Name + ":" + ReadProperties(property.GetValue(obj, null), checkedTypes);
                        }
                        else
                        {
                            // Meh
                        }
                    }
                    catch (Exception ex)
                    {
                        propertiesFound += string.Format("\nError reading {0}: {1}", property.Name, ex.Message);
                    }
                }
            }
            return propertiesFound;
        }
        private static double DumpObjects(IEnumerable<DiaObject> objects, double iType)
        {
            Logger.Log("Objects found: {0}", objects.Count());
            foreach (DiaObject o in objects)
            {
                if (!o.IsValid)
                    continue;

                string attributesFound = "";

                foreach (ActorAttributeType aType in Enum.GetValues(typeof(ActorAttributeType)))
                {
                    if (IgnoreActorAttributeTypes.Contains(aType))
                        continue;
                    try
                    {
                        iType = GetAttribute(iType, o, aType);
                    }
                    catch { }

                    if (iType > 0)
                    {
                        attributesFound += aType.ToString() + "=" + iType.ToString() + ", ";
                    }
                }

                Vector3 myPos = ZetaDia.Me.Position;
                try
                {
                    Logger.Log("\nObject ActorSNO: {0} Name: {1} Type: {2} Radius: {3:0.00} Position: {4} ({5}) Animation: {6} Distance: {7:0.0} has Attributes: {8}\n\n",
                        o.ActorSNO, o.Name, o.ActorType, o.CollisionSphere.Radius, QuestTools.GetProfilePosition(o.Position), QuestTools.GetSimplePosition(o.Position), o.CommonData.CurrentAnimation, o.Position.Distance(myPos),
                        attributesFound);
                }
                catch { }
            }
            return iType;
        }

        private static double DumpGizmos(IEnumerable<DiaGizmo> gizmos, double iType)
        {
            Logger.Log("Gizmos found: {0}", gizmos.Count());
            foreach (DiaGizmo o in gizmos)
            {
                if (!o.IsValid)
                    continue;

                if (o.ActorInfo.GizmoType == Zeta.Game.Internals.SNO.GizmoType.Trigger)
                    continue;

                if (o.ActorInfo.GizmoType == Zeta.Game.Internals.SNO.GizmoType.Checkpoint)
                    continue;

                string attributesFound = "";

                foreach (ActorAttributeType aType in Enum.GetValues(typeof(ActorAttributeType)))
                {
                    if (IgnoreActorAttributeTypes.Contains(aType))
                        continue;
                    iType = GetAttribute(iType, o, aType);
                    if (iType > 0)
                    {
                        attributesFound += aType.ToString() + "=" + iType.ToString() + ", ";
                    }
                }

                if (o is GizmoBanner)
                {
                    var banner = (GizmoBanner)o;
                    attributesFound += "BannerIndex=" + banner.BannerPlayerIndex + ", ";
                    attributesFound += "BannerACDId=" + banner.BannerACDId + ", ";
                    attributesFound += "BannerCPlayerACDGuid=" + banner.BannerPlayer.ACDGuid + ", ";
                    attributesFound += "IsBannerUsable=" + banner.IsBannerUsable + ", ";
                    attributesFound += "IsBannerPlayerInCombat=" + banner.IsBannerPlayerInCombat + ", ";
                    attributesFound += "IsMyBanner=" + (banner.BannerPlayer.ACDGuid == ZetaDia.Me.ACDGuid).ToString() + ", ";
                }

                try
                {
                    Logger.Log("\nGizmo ActorSNO: {0} Name: {1} Type: {2} Radius: {3:0.00} Position: {4} ({5}) Distance: {6:0} Animation: {7} AppearanceSNO: {8} has Attributes: {9}\n\n",
                        o.ActorSNO, o.Name, o.ActorInfo.GizmoType, o.CollisionSphere.Radius, QuestTools.GetProfilePosition(o.Position), QuestTools.GetSimplePosition(o.Position), o.Distance, o.CommonData.CurrentAnimation, o.AppearanceSNO, attributesFound);
                }
                catch { }
            }
            return iType;
        }

        private static double DumpItems(IEnumerable<DiaItem> items, double iType)
        {
            Logger.Log("Items found: {0}", items.Count());
            foreach (DiaItem o in items)
            {
                if (!o.IsValid)
                    continue;

                string attributesFound = "";

                foreach (ActorAttributeType aType in Enum.GetValues(typeof(ActorAttributeType)))
                {
                    if (IgnoreActorAttributeTypes.Contains(aType))
                        continue;
                    iType = GetAttribute(iType, o, aType);
                    if (iType > 0)
                    {
                        attributesFound += aType.ToString() + "=" + iType.ToString() + ", ";
                    }
                }

                try
                {
                    Logger.Log("\nItem ActorSNO: {0} Name: {1} Type: {2} Radius: {3:0.00} Position: {4} ({5}) Distance: {6:0} Animation: {7} AppearanceSNO: {8} has Attributes: {9}\n\n",
                        o.ActorSNO, o.Name, o.ActorInfo.GizmoType, o.CollisionSphere.Radius, QuestTools.GetProfilePosition(o.Position), QuestTools.GetSimplePosition(o.Position), o.Distance, o.CommonData.CurrentAnimation, o.AppearanceSNO, attributesFound);
                }
                catch { }
            }
            return iType;
        }

        private static double DumpPlayers(IEnumerable<DiaUnit> players, double iType)
        {
            Logger.Log("Players found: {0}", players.Count());
            HashSet<string> scannedAttributes = new HashSet<string>();
            foreach (DiaPlayer o in players)
            {
                if (!o.IsValid)
                    continue;

                string attributesFound = "", propertiesFound = "";

                foreach (ActorAttributeType aType in Enum.GetValues(typeof(ActorAttributeType)))
                {
                    if (IgnoreActorAttributeTypes.Contains(aType))
                        continue;
                    if (scannedAttributes.Contains(aType.ToString()))
                        continue;

                    double aat = o.CommonData.GetAttribute<int>(aType);

                    if (aat == 0d || aat == -1d || aat == double.NaN || aat == double.MinValue || aat == double.MaxValue)
                        continue;

                    if (aat.ToString() == "NaN")
                        continue;
                    scannedAttributes.Add(aType.ToString());

                    attributesFound += aType.ToString() + "=" + aat.ToString() + ", ";
                }

                //propertiesFound = ReadProperties<DiaPlayer>(o, null);

                try
                {
                    Logger.Log("\nPlayer ActorSNO: {0} Name: {1} Type: {2} Radius: {3:0.00} Position: {4} ({5}) RActorGUID: {6} ACDGuid: {7} SummonerId: {8} " +
                        "SummonedByAcdId: {9} Animation: {10} isHidden: {11} SNOApperance: {12} has Attributes: {13}\nProperties:\n{14}\n\n",
                    o.ActorSNO, o.Name, o.ActorInfo.GizmoType, o.CollisionSphere.Radius, QuestTools.GetProfilePosition(o.Position), QuestTools.GetSimplePosition(o.Position),
                    o.RActorGuid, o.ACDGuid, o.SummonerId, o.SummonedByACDId,
                    o.CommonData.CurrentAnimation, o.IsHidden, o.ActorInfo.SNOApperance, attributesFound, propertiesFound);
                }
                catch { }

            }
            return iType;
        }

        private static void DumpService()
        {
            string propertiesFound = "";

            Type unitType = typeof(DiaPlayer);
            propertiesFound = ReadProperties(ZetaDia.Service, null);

            try
            {
                Logger.Log("\n\nService: " + propertiesFound);
            }
            catch { }

        }

        private static double GetAttribute(double iType, DiaObject o, ActorAttributeType aType)
        {
            try
            {
                iType = (double)o.CommonData.GetAttribute<ActorAttributeType>(aType);
            }
            catch
            {
                iType = -1;
            }

            return iType;
        }
        private static double GetAttribute(DiaObject o, ActorAttributeType aType)
        {
            try
            {
                return o.CommonData.GetAttribute<double>(aType);
            }
            catch
            {
                return (double)(-1);
            }
        }
        private static List<ActorAttributeType> IgnoreActorAttributeTypes = new List<ActorAttributeType>() {
            /*
             * [QuestTools] Unit ActorSNO: 5388 
             * Name: SkeletonSummoner_B-422 
             * Type: None 
             * Radius: 9.44 
             * Position: x="293" y="258" z="-10"  (293, 258, -10) 
             * Animation: SkeletonSummoner_attack_01 has 
             * Attributes: 
             * Level=60, 
             * TeamID=10, 
             * HitpointsCur=1201855092, 
             * HitpointsMax=1206500143, 
             * HitpointsMaxTotal=1206500143, 
             * EnchantRangeMax=255, 
             * SummonerID=2037973425, 
             * LastDamageACD=2018508947, 
             * ProjectileReflectDamageScalar=1065353216, 
             * BuffVisualEffect=1, 
             * ScreenAttackRadiusConstant=1114636288, 
             * TurnRateScalar=1065353216, 
             * TurnAccelScalar=1065353216, 
             * TurnDeccelScalar=1065353216, 
             * UnequippedTime=1, 
             * CoreAttributesFromItemBonusMultiplier=1065353216, 
             * IsTemporaryLure=1, 
             * PowerPrimaryResourceCostOverride=2139095039, 
             * PowerSecondaryResourceCostOverride=2139095039, 
             * PowerChannelCostOverride=2139095039, 
             */

            ActorAttributeType.EnchantRangeMax,
            ActorAttributeType.ScreenAttackRadiusConstant,
            ActorAttributeType.TurnRateScalar,
            ActorAttributeType.TurnAccelScalar,
            ActorAttributeType.TurnDeccelScalar,
            ActorAttributeType.UnequippedTime,
            ActorAttributeType.CoreAttributesFromItemBonusMultiplier,
            ActorAttributeType.IsTemporaryLure,
            ActorAttributeType.PowerPrimaryResourceCostOverride, 
            ActorAttributeType.PowerSecondaryResourceCostOverride,
            ActorAttributeType.PowerChannelCostOverride,

        };
    }
}
