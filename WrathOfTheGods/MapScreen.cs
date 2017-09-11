﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;


using Microsoft.Xna.Framework.Graphics;
using Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using WrathOfTheGods.XMLLibrary;

namespace WrathOfTheGods
{

    /// <summary>
    /// Handles the graphical elements of the Map-level game.
    /// </summary>
    class MapScreen : ScrollingScreen
    {
        internal Texture2D Map
        {
            set
            {
                Limits = new Vector2(value.Width, value.Height);
                map = value;
            }
            private get => map;
        }
        private Texture2D map;

        internal Texture2D CityTex
        {
            set
            {
                CitySize = value.Height;
                CityGate = new Vector2(CitySize / 2, CitySize - 2);
                cityTex = value;
            }
            private get => cityTex; }
        private Texture2D cityTex;
        internal static int CitySize
        { get; private set; }
        private static Vector2 CityGate;   //the offset from the top-left (where the sprite is drawn from) to the center-bottom(ish), where paths are desired to come from

        internal Texture2D SmallPath
        { set; private get; }
        internal Texture2D LargePath
        { set; private get; }

        internal Texture2D HeroTex
        {
            set
            {
                HeroSize = new Vector2(value.Width, value.Height);
                HeroOffset = HeroSize - new Vector2(CitySize) - new Vector2(5, 10);
                heroTex = value;
            }
            private get => heroTex;
        }
        private Texture2D heroTex;
        internal static Vector2 HeroSize
        { get; private set; }
        internal static Vector2 HeroOffset
        { get; private set; }

        internal Texture2D FactionShieldTex
        { set; private get; }
        private static Vector2 ShieldOffset = new Vector2(-2, 20);
        

        MapManager mapManager;
        
        //this constructor is full of magic numbers,
        //but it doesn't really make sense to make constants that get used once to pass in to the base constructor
        public MapScreen(Vector2 screenSize) : base(new Vector2(), screenSize, 8, 1, 10, .005f)
        { }

        public void SetCities(List<City> cities)
        {
            mapManager = new MapManager(cities, new Func<Vector2, Vector2>(ConvertToLogicalSpace));
        }
        
        public override (bool updateBelow, bool shouldClose) Update(InputSet input)
        {
            if (mapManager is null)
                return (false, false);

            mapManager.Update(input);

            base.Update(input);

            return (false, false);
            //TODO: make returns actually do something.
        }

        public override void Draw(SpriteBatch drawer)
        {
            if (mapManager is null)
                return;

            //draw map
            DrawSprite(drawer, map, new Vector2(0, 0), 0);

            //draw cities, including neighbor paths & faction shields
            foreach (City city in mapManager.Cities)
            {
                DrawSprite(drawer, CityTex, city.Position, 0.5f);

                Vector2 home = city.Position + CityGate;
                foreach(City neighbor in city.GetNeighbors())
                {
                    //to prevent two paths drawing over each other
                    if(city.Position.X >= neighbor.Position.X)
                    {
                        Vector2 destination = neighbor.Position + CityGate;

                        DrawPath(drawer, SmallPath, home, destination, 0.25f);
                    }
                }

                if(city.Faction != null)
                {
                    DrawSprite(drawer, FactionShieldTex, city.Position + ShieldOffset, city.Faction.Color, 0.55f);
                }
            }

            Hero activeHero = mapManager.activeHero;

            //draw heroes, including dislocation for dragged hero
            foreach(Hero hero in mapManager.Heroes)
            {
                Vector2 position = hero.Location.Position - HeroOffset;

                if (hero == activeHero)
                    DrawSprite(drawer, HeroTex, position, mapManager.activeHeroDelta, 0.61f);
                else
                    DrawSprite(drawer, HeroTex, position, 0.6f);

            }

            //draw possible move paths for dragged hero
            if(activeHero != null)
            {
                List<City> completed = new List<City>();
                Queue<City> pathOut = new Queue<City>();

                pathOut.Enqueue(activeHero.Location);

                //breadth-first exploring
                while (pathOut.Count > 0)
                {
                    City current = pathOut.Dequeue();
                    if (completed.Contains(current))
                        continue;

                    completed.Add(current);

                    Vector2 home = current.Position + CityGate;

                    foreach(City neighbor in current.GetNeighbors())
                    {
                        if (completed.Contains(neighbor) || pathOut.Contains(neighbor))
                            continue;  //continues the inner loop

                        Vector2 destination = neighbor.Position + CityGate;

                        DrawPath(drawer, LargePath, home, destination, 0.26f);

                        if (neighbor.Faction == activeHero.Faction)
                            pathOut.Enqueue(neighbor);
                        else
                            completed.Add(neighbor);
                    }
                }
            }

           
        }

        public override bool DrawUnder()
        {
            return false;
        }

        public override bool ShouldClose()
        {
            return false;
            //TODO: it's possible there are times it's not updating it should close
            //like, if the player quicksaves from a battle or something
        }

        public override void Close()
        {
            //TODO: this
            throw new NotImplementedException();
        }
    }
}