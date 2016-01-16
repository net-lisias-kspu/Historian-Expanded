/**
 * This file is part of Historian.
 * 
 * Historian is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Historian is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Historian. If not, see <http://www.gnu.org/licenses/>.
 **/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSEA.Historian
{

    public struct CommonInfo
    {
        public Vessel Vessel;
        public Orbit Orbit;
        public double UT;
        public int[] Time;
        public ITargetable Target;

        public int Year { get { return Time[4]; } }
        public int Day { get { return Time[3] + 1; } }
        public int Hour { get { return Time[2]; } }
        public int Minute {  get { return Time[1]; } }
        public int Second { get { return Time[0]; } }
    }

    public class Text : Element
    {
        Color m_Color = Color.white;
        string m_Text = "";
        TextAnchor m_TextAnchor = TextAnchor.MiddleCenter;
        int m_FontSize = 10;
        FontStyle m_FontStyle = FontStyle.Normal;
        string m_pilotColor, m_engineerColor, m_scientistColor, m_touristColor;
        int m_baseYear;
        string m_dateFormat;
        bool m_isKerbincalendar;

        readonly Dictionary<string, Func<CommonInfo, string>> m_parsers = new Dictionary<string, Func<CommonInfo, string>>();

        readonly static string[] m_AllTraits = { "Pilot", "Engineer", "Scientist", "Tourist" };

        public Text()
        {
            InitializeParameterDictionary();
        }

        public void SetText(string text)
        {
            m_Text = text;
        }

        protected override void OnDraw(Rect bounds)
        {
            var style = new GUIStyle(GUI.skin.label);

            style.alignment = m_TextAnchor;
            style.normal.textColor = m_Color;
            style.fontSize = m_FontSize;
            style.fontStyle = m_FontStyle;
            style.richText = true;

            var content = new GUIContent();
            content.text = Parse(m_Text);

            GUI.Label(bounds, content, style);
        }

        protected override void OnLoad(ConfigNode node)
        {

            m_Color = node.GetColor("Color", Color.white);
            m_Text = node.GetString("Text", "");
            m_TextAnchor = node.GetEnum("TextAnchor", TextAnchor.MiddleCenter);
            m_FontSize = node.GetInteger("FontSize", 10);
            m_FontStyle = node.GetEnum("FontStyle", FontStyle.Normal);

            m_pilotColor = node.GetString("PilotColor", "clear");
            m_engineerColor = node.GetString("EngineerColor", "clear");
            m_scientistColor = node.GetString("ScientistColor", "clear");
            m_touristColor = node.GetString("TouristColor", "clear");

            m_isKerbincalendar = GameSettings.KERBIN_TIME;

            m_baseYear = node.GetInteger("BaseYear", m_isKerbincalendar ? 1 : 1940);
            m_dateFormat = node.GetString("DateFormat", CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern);
        }

        void InitializeParameterDictionary()
        {
            m_parsers.Add("N", NewLineParser);
            m_parsers.Add("Custom", CustomParser);
            m_parsers.Add("Date", DateParser);
            m_parsers.Add("UT", UTParser);
            m_parsers.Add("T+", TPlusParser);
            m_parsers.Add("Year", YearParser);
            m_parsers.Add("Day", DayParser);
            m_parsers.Add("Hour", HourParser);
            m_parsers.Add("Minute", MinuteParser);
            m_parsers.Add("Second", SecondParser);
            m_parsers.Add("Vessel", VesselParser);
            m_parsers.Add("Body", BodyParser);
            m_parsers.Add("Biome", BiomeParser);
            m_parsers.Add("Situation", SituationParser);
            m_parsers.Add("LandingZone", LandingZoneParser);
            m_parsers.Add("Latitude", LatitudeParser);
            m_parsers.Add("Longitude", LongitudeParser);
            m_parsers.Add("Heading", HeadingParser);
            m_parsers.Add("Mach", MachParser);
            m_parsers.Add("Speed", SpeedParser);
            m_parsers.Add("SrfSpeed", SurfaceSpeedParser);
            m_parsers.Add("OrbSpeed", OrbitalSpeedParser);
            m_parsers.Add("Ap", ApParser);
            m_parsers.Add("Pe", PeParser);
            m_parsers.Add("Inc", IncParser);
            m_parsers.Add("Ecc", EccParser);
            m_parsers.Add("LAN", LanParser);
            m_parsers.Add("ArgPe", ArgPeParser);
            m_parsers.Add("Period", PeriodParser);
            m_parsers.Add("Orbit", OrbitParser);
            m_parsers.Add("Crew", CrewParser);
            m_parsers.Add("CrewShort", CrewShortParser);
            m_parsers.Add("CrewList", CrewListParser);
            m_parsers.Add("Pilots", PilotsParser);
            m_parsers.Add("PilotsShort", PilotsShortParser);
            m_parsers.Add("PilotsList", PilotsListParser);
            m_parsers.Add("Engineers", EngineersParser);
            m_parsers.Add("EngineersShort", EngineersShortParser);
            m_parsers.Add("EngineersList", EngineersListParser);
            m_parsers.Add("Scientists", ScientistsParser);
            m_parsers.Add("ScientistsShort", ScientistsShortParser);
            m_parsers.Add("ScientistsList", ScientistsListParser);
            m_parsers.Add("Tourists", TouristsParser);
            m_parsers.Add("TouristsShort", TouristsShortParser);
            m_parsers.Add("TouristsList", TouristsListParser);
            m_parsers.Add("Target", TargetParser);
        }

        protected string Parse(string text)
        {
            var result = new StringBuilder();

            // get common data sources
            var ut = Planetarium.GetUniversalTime();
            var time = m_isKerbincalendar ? KSPUtil.GetKerbinDateFromUT((int)ut) : KSPUtil.GetEarthDateFromUT((int)ut);
            var vessel = FlightGlobals.ActiveVessel;
            var orbit = vessel?.GetOrbit();
            var target = vessel?.targetObject;

            var info = new CommonInfo
            {
                Vessel = vessel,
                Orbit = orbit,
                Time = time,
                UT = ut,
                Target = target
            };

            // scan template text string for parameter tokens
            int i = 0, tokenLen;
            while (i < text.Length)
            {
                char ch = text[i];
                if (ch == '<')
                {
                    // possible token found
                    tokenLen = GetTokenLength(text, i);
                    if (tokenLen >= 0)
                    {
                        // extract token
                        var token = text.Substring(i + 1, tokenLen);
                        // check if recognised
                        if (m_parsers.ContainsKey(token))
                        {
                            // run parser for matching token
                            result.Append(m_parsers[token](info));
                        }
                        else
                        {
                            // token not found copy as literal
                            result.Append("<");
                            result.Append(token);
                            result.Append(">");
                        }
                        // include < and > in counted tokenlength
                        tokenLen += 2;
                    }
                    else
                    {
                        // no end token found treat as literal
                        tokenLen = 1;
                        result.Append(ch);
                    }
                }
                else
                {
                    // literal
                    tokenLen = 1;
                    result.Append(ch);
                }
                i += tokenLen;
            }

            return result.ToString();
        }

        private int GetTokenLength(string text, int pos)
        {
            return text.IndexOf('>', pos) - pos - 1;
        }


        string NewLineParser(CommonInfo info) => Environment.NewLine;

        string CustomParser(CommonInfo info) => Parse(Historian.Instance.GetConfiguration().CustomText.Replace("<Custom>", "")); // avoid recurssion.

        string DateParser(CommonInfo info) =>
            m_isKerbincalendar
                ? info.Time.FormattedDate(m_dateFormat, m_baseYear)
                : new DateTime(info.Year + m_baseYear, 1, 1, info.Hour, info.Minute, info.Second).AddDays(info.Day - 1).ToString(m_dateFormat);

        string UTParser(CommonInfo info) => $"Y{info.Year + m_baseYear}, D{(info.Day):D3}, {info.Hour}:{info.Minute:D2}:{info.Second:D2}";

        string YearParser(CommonInfo info) => (info.Year + m_baseYear).ToString();

        string DayParser(CommonInfo info) => info.Day.ToString();

        string HourParser(CommonInfo info) => info.Hour.ToString();

        string MinuteParser(CommonInfo info) => info.Minute.ToString();

        string SecondParser(CommonInfo info) => info.Second.ToString();

        string TPlusParser(CommonInfo info)
        {
            if (info.Vessel != null)
            {
                var t = KSPUtil.GetKerbinDateFromUT((int)info.Vessel.missionTime);
                return (t[4] > 0)
                    ? $"T+ {t[4] + 1}y, {t[3] + 1}d, {t[2]:D2}:{t[1]:D2}:{t[0]:D2}"
                    : (t[3] > 0)
                        ? $"T+ {t[3] + 1}d, {t[2]:D2}:{t[1]:D2}:{t[0]:D2}"
                        : $"T+ {t[2]:D2}:{t[1]:D2}:{t[0]:D2}";
            }
            return "";
        }

        string VesselParser(CommonInfo info) => info.Vessel?.vesselName;

        string BodyParser(CommonInfo info) => info.Vessel != null ? Planetarium.fetch.CurrentMainBody.bodyName : "";

        string SituationParser(CommonInfo info)
            => (info.Vessel == null) 
                    ? "" 
                    : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(info.Vessel.situation.ToString().Replace("_", "-").ToLower());

        string BiomeParser(CommonInfo info)
            => (info.Vessel == null) 
                    ? "" 
                    : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ScienceUtil.GetExperimentBiome(info.Vessel.mainBody, info.Vessel.latitude, info.Vessel.longitude).ToLower());

        string LandingZoneParser(CommonInfo info)
        {
            if (info.Vessel == null)
                return "";
            var landedAt = (string.IsNullOrEmpty(info.Vessel.landedAt))
                ? ScienceUtil.GetExperimentBiome(info.Vessel.mainBody, info.Vessel.latitude, info.Vessel.longitude)
                : Vessel.GetLandedAtString(info.Vessel.landedAt); // http://forum.kerbalspaceprogram.com/threads/123896-Human-Friendly-Landing-Zone-Title
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(landedAt.ToLower());
        }

        string LatitudeParser(CommonInfo info) => info.Vessel == null ? "" : info.Vessel.latitude.ToString("F3");

        string LongitudeParser(CommonInfo info) => info.Vessel == null ? "" : info.Vessel.longitude.ToString("F3");

        string HeadingParser(CommonInfo info) => FlightGlobals.ship_heading.ToString("F1");

        string AltitudeParser(CommonInfo info) => info.Vessel == null ? "" : SimplifyDistance(info.Vessel.altitude);

        string MachParser(CommonInfo info) => info.Vessel == null ? "" : info.Vessel.mach.ToString("F1");

        string SpeedParser(CommonInfo info) => info.Vessel == null ? "" : SimplifyDistance(info.Vessel.srfSpeed) + @"/s";

        string SurfaceSpeedParser(CommonInfo info) => info.Vessel == null ? "" : SimplifyDistance(info.Vessel.srfSpeed) + @"/s";

        string OrbitalSpeedParser(CommonInfo info) => info.Orbit == null ? "" : SimplifyDistance(info.Orbit.orbitalSpeed) + @"/s";

        string ApParser(CommonInfo info) => info.Orbit == null ? "" : SimplifyDistance(info.Orbit.ApA);

        string PeParser(CommonInfo info) => info.Orbit == null ? "" : SimplifyDistance(info.Orbit.PeA);

        string IncParser(CommonInfo info) => info.Orbit == null ? "" : info.Orbit.inclination.ToString("F2") + "�";

        string EccParser(CommonInfo info) => info.Orbit == null ? "" : info.Orbit.eccentricity.ToString("F3");

        string LanParser(CommonInfo info) => info.Orbit == null ? "" : info.Orbit.LAN.ToString("F1") + "�";

        string ArgPeParser(CommonInfo info) => info.Orbit == null ? "" : info.Orbit.argumentOfPeriapsis.ToString("F1") + "�";

        string PeriodParser(CommonInfo info)
        {
            if (info.Orbit == null)
                return "";

            var period = info.Orbit.period;
            var t = m_isKerbincalendar
                ? KSPUtil.GetKerbinDateFromUT((int)period)
                : KSPUtil.GetEarthDateFromUT((int)period);
            return (t[4] > 0)
                     ? $"{t[4] + 1}y, {t[3] + 1}d, {t[2]:D2}:{t[1]:D2}:{t[0]:D2}"
                     : (t[3] > 0)
                         ? $"{t[3] + 1}d, {t[2]:D2}:{t[1]:D2}:{t[0]:D2}"
                         : $"{t[2]:D2}:{t[1]:D2}:{t[0]:D2}";
        }

        string OrbitParser(CommonInfo info)
            => info.Orbit == null ? "" : $"{SimplifyDistance(info.Orbit.ApA)} x {SimplifyDistance(info.Orbit.PeA)}";

        string CrewParser(CommonInfo info)
            => GenericCrewParser(info.Vessel, isList: false, isShort: false, traits: m_AllTraits);

        string CrewShortParser(CommonInfo info)
            => GenericCrewParser(info.Vessel, isList: false, isShort: true, traits: m_AllTraits);

        string CrewListParser(CommonInfo info)
            => GenericCrewParser(info.Vessel, isList: true, isShort: false, traits: m_AllTraits);

        string PilotsParser(CommonInfo info)
            => GenericCrewParser(info.Vessel, isList: false, isShort: false, traits: new string[] { "Pilot" });

        string PilotsShortParser(CommonInfo info)
            => GenericCrewParser(info.Vessel, isList: false, isShort: true, traits: new string[] { "Pilot" });

        string PilotsListParser(CommonInfo info)
            => GenericCrewParser(info.Vessel, isList: true, isShort: false, traits: new string[] { "Pilot" });

        string EngineersParser(CommonInfo info)
            => GenericCrewParser(info.Vessel, isList: false, isShort: false, traits: new string[] { "Engineer" });

        string EngineersShortParser(CommonInfo info)
            => GenericCrewParser(info.Vessel, isList: false, isShort: true, traits: new string[] { "Engineer" });

        string EngineersListParser(CommonInfo info)
            => GenericCrewParser(info.Vessel, isList: true, isShort: false, traits: new string[] { "Engineer" });

        string ScientistsParser(CommonInfo info)
            => GenericCrewParser(info.Vessel, isList: false, isShort: false, traits: new string[] { "Scientist" });

        string ScientistsShortParser(CommonInfo info)
            => GenericCrewParser(info.Vessel, isList: false, isShort: true, traits: new string[] { "Scientist" });

        string ScientistsListParser(CommonInfo info)
            => GenericCrewParser(info.Vessel, isList: true, isShort: false, traits: new string[] { "Scientist" });

        string TouristsParser(CommonInfo info)
            => GenericCrewParser(info.Vessel, isList: false, isShort: false, traits: new string[] { "Tourist" });

        string TouristsShortParser(CommonInfo info)
            => GenericCrewParser(info.Vessel, isList: false, isShort: true, traits: new string[] { "Tourist" });

        string TouristsListParser(CommonInfo info)
            => GenericCrewParser(info.Vessel, isList: true, isShort: false, traits: new string[] { "Tourist" });

        string TargetParser(CommonInfo info) => info.Target == null ? "" : info.Target.GetName();


        string GenericCrewParser(Vessel vessel, bool isList, bool isShort, string[] traits)
        {
            if (vessel == null || vessel.isEVA || !vessel.isCommandable)
                return "";

            var isSingleTrait = traits.Length == 1;

            Func<string, string> nameFilter = x => x;
            if (isShort) nameFilter = x => x.Replace(" Kerman", "");

            var crew = vessel.GetVesselCrew()
                .Where(c => traits.Contains(c.trait))
                .Select(c => TraitColor(c.trait) + nameFilter(c.name) + "</color>")
                .ToArray();

            if (crew.Length <= 0)
                return isSingleTrait ? "None" : "Unmanned";

            if (isList)
                return "� " + string.Join(Environment.NewLine + "� ", crew);

            return string.Join(", ", crew) + (isShort ? (isSingleTrait ? TraitColor(traits[0]) + " Kerman</color>" : " Kerman") : "");
        }


        protected string TraitColor(string trait)
        {
            switch (trait)
            {
                case "Pilot":
                    return "<color=" + m_pilotColor + ">";
                case "Engineer":
                    return "<color=" + m_engineerColor + ">";
                case "Scientist":
                    return "<color=" + m_scientistColor + ">";
                case "Tourist":
                    return "<color=" + m_touristColor + ">";
                default:
                    return "<color=clear>";
            }
        }

        static readonly string[] m_units = { "m", "km", "Mm", "Gm", "Tm", "Pm" };

        protected static string SimplifyDistance(double meters)
        {
            double d = meters;
            int i = 0;

            while (d > 1000.0)
            {
                d /= 1000.0f;
                ++i;
            }

            return $"{d:F1} {m_units[i]}";
        }

    }
}