﻿using System;
using System.Diagnostics.Eventing.Reader;
using System.Drawing.Printing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using Color = System.Drawing.Color;
using JustOlaf;

namespace JustTrundle
{
    internal class Program
    {
        public const string ChampName = "Trundle";
        public static HpBarIndicator Hpi = new HpBarIndicator();
        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Spell Smite;
        public static int SpellRangeTick;
        //Credits to Kurisu for Smite Stuff :^)
        public static readonly int[] SmitePurple = { 3713, 3726, 3725, 3726, 3723 };
        public static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719 };
        public static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714 };
        public static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707 };

        private static SpellSlot Ignite;
        private static SpellSlot smiteSlot;
        private static int LastCast;
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnLoad;

        }

        private static void OnLoad(EventArgs args)
        {
            if (player.ChampionName != ChampName)
                return;

            Notifications.AddNotification("JustTrundle - [V.1.0.0.0]", 8000);

            Killsteal();

            //Ability Information - Range - Variables.
            Q = new Spell(SpellSlot.Q, 125f);
            W = new Spell(SpellSlot.W, 900f);
            E = new Spell(SpellSlot.E, 1000f);
            E.SetSkillshot(.5f, 188f, 1600f, false, SkillshotType.SkillshotCircle);
            R = new Spell(SpellSlot.R, 700f);


            Config = new Menu("JustTrundle", "Trundle", true);
            Orbwalker = new Orbwalking.Orbwalker(Config.AddSubMenu(new Menu("[JT]: Orbwalker", "Orbwalker")));
            TargetSelector.AddToMenu(Config.AddSubMenu(new Menu("[JT]: Target Selector", "Target Selector")));

            //COMBOMENU

            var combo = Config.AddSubMenu(new Menu("[JT]: Combo Settings", "Combo Settings"));
            var harass = Config.AddSubMenu(new Menu("JT]: Harass Settings", "Harass Settings"));
            var drawing = Config.AddSubMenu(new Menu("[JT]: Draw Settings", "Draw Settings"));

            combo.SubMenu("[SBTW] ManaManager").AddItem(new MenuItem("qmana", "[Q] Mana %").SetValue(new Slider(10, 100, 0)));
            combo.SubMenu("[SBTW] ManaManager").AddItem(new MenuItem("wmana", "[W] Mana %").SetValue(new Slider(10, 100, 0)));
            combo.SubMenu("[SBTW] ManaManager").AddItem(new MenuItem("emana", "[E] Mana %").SetValue(new Slider(10, 100, 0)));
            combo.SubMenu("[SBTW] ManaManager").AddItem(new MenuItem("rmana", "[R] Mana %").SetValue(new Slider(15, 100, 0)));

            combo.SubMenu("[Q] Settings").AddItem(new MenuItem("UseQ", "Use Q").SetValue(true));
            combo.SubMenu("[W] Settings").AddItem(new MenuItem("UseW", "Use W").SetValue(true));
            combo.SubMenu("[E] Settings").AddItem(new MenuItem("UseE", "Use E").SetValue(true));
            combo.SubMenu("[R] Settings").AddItem(new MenuItem("UseR", "Use R (TOGGLE) ").SetValue(new KeyBind('K', KeyBindType.Toggle)));
            combo.SubMenu("[R] Settings").AddItem(new MenuItem("manualr", "Cast R Manual").SetValue(new KeyBind('R', KeyBindType.Press)));
            combo.SubMenu("[R] Settings").AddItem(new MenuItem("DontUlt", "Dont use R on"));
            combo.SubMenu("[R] Settings").AddItem(new MenuItem("sep0", "===L#==="));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != player.Team))
            {
                combo.SubMenu("[R] Settings").AddItem(new MenuItem("DontUlt" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));
            }
            combo.SubMenu("[R] Settings").AddItem(new MenuItem("sep1", "===L#==="));
            combo.SubMenu("Smite Settings").AddItem(new MenuItem("useSmiteCombo", "Use Smite On Combo")).SetValue(true);
            
            
            combo.SubMenu("Item Settings")
                .AddItem(new MenuItem("useGhostblade", "Use Youmuu's Ghostblade").SetValue(true));
            combo.SubMenu("Item Settings")
                .AddItem(new MenuItem("UseBOTRK", "Use Blade of the Ruined King").SetValue(true));
            combo.SubMenu("Item Settings")
                .AddItem(new MenuItem("eL", "  Enemy HP Percentage").SetValue(new Slider(80, 100, 0)));
            combo.SubMenu("Item Settings")
                .AddItem(new MenuItem("oL", "  Own HP Percentage").SetValue(new Slider(65, 100, 0)));
            combo.SubMenu("Item Settings").AddItem(new MenuItem("UseBilge", "Use Bilgewater Cutlass").SetValue(true));
            combo.SubMenu("Item Settings")
                .AddItem(new MenuItem("HLe", "  Enemy HP Percentage").SetValue(new Slider(80, 100, 0)));
            combo.SubMenu("Summoner Settings").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));

            //LANECLEARMENU
            Config.SubMenu("[JT]: Laneclear Settings")
                .AddItem(new MenuItem("laneQ", "Use Q").SetValue(true));
            Config.SubMenu("[JT]: Laneclear Settings")
                .AddItem(new MenuItem("laneW", "Use W").SetValue(true));
            Config.SubMenu("[JT]: Laneclear Settings")
                .AddItem(new MenuItem("laneclearmana", "Mana Percentage").SetValue(new Slider(30, 100, 0)));

            //JUNGLEFARMMENU
            Config.SubMenu("[JT]: Jungle Settings")
                .AddItem(new MenuItem("jungleQ", "Use Q").SetValue(true));
            Config.SubMenu("[JT]: Jungle Settings")
                .AddItem(new MenuItem("jungleW", "Use W").SetValue(true));
            Config.SubMenu("[JT]: Jungle Settings")
                .AddItem(new MenuItem("jungleclearmana", "Mana Percentage").SetValue(new Slider(30, 100, 0)));

            //KSMENU
            Config.SubMenu("[JT]: Killsteal Settings").AddItem(new MenuItem("ksQ", "Use Q For KS").SetValue(true));
           
            drawing.AddItem(new MenuItem("Draw_Disabled", "Disable All Spell Drawings").SetValue(false));
            drawing.AddItem(new MenuItem("Qdraw", "Draw Q Range").SetValue(new Circle(true, Color.Orange)));
            drawing.AddItem(new MenuItem("Edraw", "Draw E Range").SetValue(new Circle(true, Color.AntiqueWhite)));
            drawing.AddItem(new MenuItem("Rdraw", "Draw R Range").SetValue(new Circle(true, Color.Red)));

            harass.AddItem(new MenuItem("harassQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("harassW", "Use W").SetValue(true));
            harass.AddItem(new MenuItem("harassE", "Use E").SetValue(true));
            harass.AddItem(new MenuItem("harassmana", "Mana Percentage").SetValue(new Slider(30, 100, 0)));

            Config.SubMenu("[JT]: Misc Settings").AddItem(new MenuItem("DrawD", "Damage Indicator").SetValue(true));
            Config.SubMenu("[JT]: Misc Settings").AddItem(new MenuItem("interrupt", "Interrupt Spells").SetValue(true));
            Config.SubMenu("[JT]: Misc Settings").AddItem(new MenuItem("antigap", "AntiGapCloser").SetValue(true));

            Drawing.OnDraw += OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnEndScene += OnEndScene;
            Interrupter2.OnInterruptableTarget += Interrupter_OnPosibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += AntiGapCloser_OnEnemyGapcloser;


        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            throw new NotImplementedException();
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (E.IsReady() && unit.IsValidTarget(E.Range) && Config.Item("interrupt").GetValue<bool>())
                E.CastIfHitchanceEquals(unit, HitChance.Medium);
        }

        private static void AntiGapCloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (E.IsReady() && gapcloser.Sender.IsValidTarget(E.Range) && Config.Item("antigap").GetValue<bool>())
                E.CastIfHitchanceEquals(gapcloser.Sender, HitChance.Medium);
        }

        public static string GetSmiteType()
        {
            if (SmiteBlue.Any(id => Items.HasItem(id)))
            {
                return "s5_summonersmiteplayerganker";
            }
            if (SmiteRed.Any(id => Items.HasItem(id)))
            {
                return "s5_summonersmiteduel";
            }
            if (SmiteGrey.Any(id => Items.HasItem(id)))
            {
                return "s5_summonersmitequick";
            }
            if (SmitePurple.Any(id => Items.HasItem(id)))
            {
                return "itemsmiteaoe";
            }
            return "summonersmite";
        }

        public static void GetSmiteSlot()
        {
            foreach (
                var spell in
                    ObjectManager.Player.Spellbook.Spells.Where(
                        spell => String.Equals(spell.Name, GetSmiteType(), StringComparison.CurrentCultureIgnoreCase)))
            {
                smiteSlot = spell.Slot;
                Smite = new Spell(smiteSlot, 700);
                return;
            }
        }


        private static void OnEndScene(EventArgs args)
        {
            if (Config.SubMenu("[JT]: Misc Settings").Item("DrawD").GetValue<bool>())
            {
                foreach (var enemy in
                    ObjectManager.Get<Obj_AI_Hero>().Where(ene => !ene.IsDead && ene.IsEnemy && ene.IsVisible))
                {
                    Hpi.unit = enemy;
                    Hpi.drawDmg(CalcDamage(enemy), Color.Green);
                }
            }
        }

        private static void combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (target == null || !target.IsValidTarget()) //if there is no target or target isn't valid it will return; (It won't combo)
                return;

            if (Config.Item("useSmiteCombo").GetValue<bool>())
            {
                Smite.Cast(target);
            }

            var wmana = Config.Item("wmana").GetValue<Slider>().Value;

            if (W.IsReady() && target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(player)) && Config.Item("UseW").GetValue<bool>())
                W.Cast();

            var emana = Config.Item("emana").GetValue<Slider>().Value;

            if (E.IsReady() && target.IsValidTarget(E.Range) && player.ManaPercentage() >= emana)
                E.CastIfHitchanceEquals(target, HitChance.High);

            var qmana = Config.Item("qmana").GetValue<Slider>().Value;

            if (Q.IsReady() && Config.Item("UseQ").GetValue<bool>() && target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(player))
            && player.ManaPercentage() >= qmana)
                Q.CastOnUnit(target);

            if (Config.Item("manualr").GetValue<KeyBind>().Active && R.IsReady())
                R.CastOnUnit(target);
            
            if (R.IsReady() && target.IsValidTarget(R.Range) && Config.Item("UseR").GetValue<bool>())
            {
                if (Config.Item("DontUlt" + target.BaseSkinName) != null &&
                    Config.Item("DontUlt" + target.BaseSkinName).GetValue<bool>() == false)
                    R.CastOnUnit(target);
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                items();


        }

        private static int CalcDamage(Obj_AI_Base target)
        {
            //The only dmg spells olaf has are E and Q (Added those and removed R/W)
            var aa = player.GetAutoAttackDamage(target, true) * (1 + player.Crit);
            var damage = aa;
            Ignite = player.GetSpellSlot("summonerdot");

            if (Ignite.IsReady())
                damage += player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);

            if (Items.HasItem(3153) && Items.CanUseItem(3153))
                damage += player.GetItemDamage(target, Damage.DamageItems.Botrk); //ITEM BOTRK

            if (Items.HasItem(3144) && Items.CanUseItem(3144))
                damage += player.GetItemDamage(target, Damage.DamageItems.Bilgewater); //ITEM BOTRK

            if (R.IsReady() && Config.Item("UseR").GetValue<bool>()) // rdamage
            {
                if (R.IsReady())
                {
                    damage += R.GetDamage(target);
                }
            }

            if (Q.IsReady() && Config.Item("UseQ").GetValue<KeyBind>().Active) // qdamage
            {

                damage += Q.GetDamage(target);
            }
            return (int)damage;
        }

        private static float IgniteDamage(Obj_AI_Hero target)
        {
            if (Ignite == SpellSlot.Unknown || player.Spellbook.CanUseSpell(Ignite) != SpellState.Ready)
                return 0f;
            return (float)player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
        }

        private static void Killsteal()
        {
            foreach (Obj_AI_Hero target in
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(hero => hero.IsValidTarget(Q.Range) && !hero.HasBuffOfType(BuffType.Invulnerability) && hero.IsEnemy)
                )
            {
                var qDmg = player.GetSpellDamage(target, SpellSlot.Q);
                if (Config.Item("ksQ").GetValue<bool>() && target.IsValidTarget(Q.Range) && target.Health <= qDmg)
                {
                    Q.CastOnUnit(target);
                }
            }
         }

        private static void items()
        {
            Ignite = player.GetSpellSlot("summonerdot");
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (target == null || !target.IsValidTarget())
                return;

            var botrk = ItemData.Blade_of_the_Ruined_King.GetItem();
            var Ghost = ItemData.Youmuus_Ghostblade.GetItem();
            var cutlass = ItemData.Bilgewater_Cutlass.GetItem();

            if (botrk.IsReady() && botrk.IsOwned(player) && botrk.IsInRange(target)
            && target.HealthPercentage() <= Config.Item("eL").GetValue<Slider>().Value
            && Config.Item("UseBOTRK").GetValue<bool>())

                botrk.Cast(target);

            if (botrk.IsReady() && botrk.IsOwned(player) && botrk.IsInRange(target)
                && player.HealthPercentage() <= Config.Item("oL").GetValue<Slider>().Value
                && Config.Item("UseBOTRK").GetValue<bool>())

                botrk.Cast(target);

            if (cutlass.IsReady() && cutlass.IsOwned(player) && cutlass.IsInRange(target) &&
                target.HealthPercentage() <= Config.Item("HLe").GetValue<Slider>().Value
                && Config.Item("UseBilge").GetValue<bool>())

                cutlass.Cast(target);

            if (Ghost.IsReady() && Ghost.IsOwned(player) && target.IsValidTarget(E.Range)
                && Config.Item("useGhostblade").GetValue<bool>())

                Ghost.Cast();

            if (player.Distance(target.Position) <= 600 && IgniteDamage(target) >= target.Health &&
                Config.Item("UseIgnite").GetValue<bool>())
                player.Spellbook.CastSpell(Ignite, target);
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    Laneclear();
                    Jungleclear();
                    break;
            }
        }

        private static void harass()
        {
            var harassmana = Config.Item("harassmana").GetValue<Slider>().Value;
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            if (target == null || !target.IsValidTarget())
                return;

            if (Q.IsReady()
                && Config.Item("hQ").GetValue<bool>()
                && target.IsValidTarget(Q.Range)
                && player.ManaPercentage() >= harassmana)

                Q.CastOnUnit(target);

            if (W.IsReady()
                && Config.Item("hW").GetValue<bool>()
                && player.ManaPercentage() >= harassmana)

                W.Cast();

            if (E.IsReady()
                && Config.Item("hE").GetValue<bool>()
                && target.IsValidTarget(E.Range)
                && player.ManaPercentage() >= harassmana)

               E.CastIfHitchanceEquals(target, HitChance.High);
        }

        private static void Laneclear()
        {
            var lanemana = Config.Item("laneclearmana").GetValue<Slider>().Value;
            
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear
                && Config.Item("laneQ").GetValue<bool>()
                && player.ManaPercentage() >= lanemana)

                Q.Cast(minion);

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear
               && Config.Item("laneW").GetValue<bool>()
               && player.ManaPercentage() >= lanemana)

                W.Cast();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear
                && Config.Item("laneE").GetValue<bool>()
                && player.ManaPercentage() >= lanemana)

                E.CastOnUnit(minion);

        }


        private static void Jungleclear()
        {
            var jlanemana = Config.Item("jungleclearmana").GetValue<Slider>().Value;
            
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear
                && Config.Item("jungleW").GetValue<bool>()
                && player.ManaPercentage() >= jlanemana)

                W.Cast();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear
                && Config.Item("jungleQ").GetValue<bool>()
                && player.ManaPercentage() >= jlanemana)

                Q.CastOnUnit(minion);
           }

        private static void OnDraw(EventArgs args)
        {
            {

            }

            //Draw If R is enabled
            var pos = Drawing.WorldToScreen(ObjectManager.Player.Position);

            //if (Config.Item("UseR").GetValue<KeyBind>().Active)
                Drawing.DrawText(pos.X - 50, pos.Y + 50, Color.Gold, "[R] is Enabled!");


            if (Config.Item("Draw_Disabled").GetValue<bool>())
                return;

            if (Config.Item("Qdraw").GetValue<Circle>().Active)
                if (Q.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, Q.IsReady() ? Config.Item("Qdraw").GetValue<Circle>().Color : Color.Red);

            if (Config.Item("Edraw").GetValue<Circle>().Active)
                if (E.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range - 1,
                        E.IsReady() ? Config.Item("Edraw").GetValue<Circle>().Color : Color.Red);

            var orbtarget = Orbwalker.GetTarget();
            Render.Circle.DrawCircle(orbtarget.Position, 100, Color.DarkOrange, 10);
        }



        public static Obj_AI_Base minion { get; set; }
    }
}