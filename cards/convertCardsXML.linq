<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.XML.dll</Reference>
  <Namespace>System.Xml.Serialization</Namespace>
</Query>

static bool downloadFiles = true;
static bool forceSpoilers = false;

static string path = Path.GetDirectoryName(Util.CurrentQueryPath);
static string baseUrl = @"https://spellstone.synapse-games.com/assets";


static System.Net.WebClient webClient = new System.Net.WebClient();
static HashSet<string> g_unitIDs;


static System.Xml.Serialization.XmlSerializer unitDeserializer = new System.Xml.Serialization.XmlSerializer(typeof(unit));
static System.Xml.Serialization.XmlSerializer bgeDeserializer = new System.Xml.Serialization.XmlSerializer(typeof(battleground));

void Main()
{
	string xmlFile;

	HashSet<string> existingUnits = new HashSet<string>(
		LoadUnits("cards_heroes.xml")
		.Union(LoadUnits("cards_premium_aether.xml"))
		.Union(LoadUnits("cards_premium_chaos.xml"))
		.Union(LoadUnits("cards_premium_wyld.xml"))
		.Union(LoadUnits("cards_reward.xml"))
		.Union(LoadUnits("cards_shard_cards.xml"))
		.Union(LoadUnits("cards_special.xml"))
		.Union(LoadUnits("cards_standard.xml"))
		.Union(LoadUnits("cards_story.xml")));


	HashSet<string> newUnits = new HashSet<string>();

	Normalize("arena.xml", downloadFiles);
	Normalize("achievements.xml", downloadFiles);
	Normalize("battleground_effects.xml", downloadFiles);
	Normalize("campaigns.xml", downloadFiles);
	Normalize("cards.xml", downloadFiles);
	Normalize("cards_config.xml", downloadFiles);
	Normalize("cards_heroes.xml", downloadFiles);
	Normalize("cards_premium_aether.xml", downloadFiles);
	Normalize("cards_premium_chaos.xml", downloadFiles);
	Normalize("cards_premium_wyld.xml", downloadFiles);
	Normalize("cards_reward.xml", downloadFiles);
	Normalize("cards_shard_cards.xml", downloadFiles);
	Normalize("cards_special.xml", downloadFiles);
	Normalize("cards_standard.xml", downloadFiles);
	Normalize("cards_story.xml", downloadFiles);
	Normalize("fusion_recipes_cj2.xml", downloadFiles);
	Normalize("guilds.xml", downloadFiles);
	Normalize("guide.xml", downloadFiles);
	Normalize("levels.xml", downloadFiles);
	Normalize("market.xml", downloadFiles);
	Normalize("missions.xml", downloadFiles);
	Normalize("missions_event.xml", downloadFiles);
	Normalize("passive_missions.xml", downloadFiles);
	Normalize("tutorial1.xml", downloadFiles);

	StringBuilder sbJSON = new StringBuilder();
	List<unit> units = new List<unit>();

	var pictures = new Dictionary<string, string>();
	var noImage = new List<string>();
	var notFound = new Dictionary<string, string>();

	var missionsXML = XDocument.Load(Path.Combine(path, "missions.xml")).Descendants("mission")
	.Union(XDocument.Load(Path.Combine(path, "missions_event.xml")).Descendants("mission"));
	var missionPortraits = missionsXML.SelectMany(node => node.Descendants("commander")).ToLookup(node => node.Value).Where(l => l.Key.Length > 0).Select(l => l.Key);
	foreach (var portrait in missionPortraits)
	{
		var imageName = "portrait_" + portrait.Replace("Portrait_", "");
		var imageFile = Path.Combine(path, @"..\res\cardImages\", imageName + ".png");
		if (!File.Exists(imageFile))
		{
			notFound.Add(imageName, "???");
		}
	}

	var skillMappings = new Dictionary<string, string>()
	{
		// Toggles
		{"silence", "toggle"},
		{"taunt", "toggle"},
		// Passives
		{"absorb", "passive"},
		{"armored", "passive"},
		{"berserk", "passive"},
		{"corrosive", "passive"},
		{"counter", "passive"},
		{"counterburn", "passive"},
		{"daze", "passive"},
		{"evade", "passive"},
		{"fury", "passive"},
		{"leech", "passive"},
		{"nullify", "passive"},
		{"pierce", "passive"},
		{"poison", "passive"},
		{"regenerate", "passive"},
		{"reinforce", "passive"},
		{"valor", "passive"},
		{"venom", "passive"},
		// Flurry
		{"flurry", "flurry"},
		// On Death
		{"unearth", "onDeath"},
		{"reanimate", "onDeath"},
		// Early Activation
		{"barrage", "earlyActivation"},
		{"enhance", "earlyActivation"},
		{"enlarge", "earlyActivation"},
		{"enrage", "earlyActivation"},
		{"fervor", "earlyActivation"},
		{"imbue", "earlyActivation"},
		{"legion", "earlyActivation"},
		{"mark", "earlyActivation"},
		{"rally", "earlyActivation"},
		// Activation
		{"burn", "activation"},
		{"enfeeble", "activation"},
		{"evadebarrier", "activation"},
		{"frost", "activation"},
		{"heal", "activation"},
		{"intensify", "activation"},
		{"ignite", "activation"},
		{"jam", "activation"},
		{"protect", "activation"},
		{"protect_ice", "activation"},
		{"scorchbreath", "activation"},
		{"strike", "activation"},
		{"weaken", "activation"}
	};

	var skillRenames = new Dictionary<string, string>()
	{
		{"counterburn", "Emberhide"}
	};

	var skillIconChanges = new Dictionary<string, string>()
	{
		{"reinforce", "reinforce"}
	};

	var iconRemappings = new Dictionary<string, string>()
	{
		{"mark", "eagle_eye"},
		{"barrage", "barrage"},
		{"protect_ice", "iceshatter"},
		{"poisonstrike", "poison_bolt"},
		{"counterburn", "counterburn"},
		{"slow", "bind"},
		{"enlarge", "empower"}
	};
	
	var mapped = new Dictionary<string, string>();

	var skillIconNames = new List<string>();
	var skillFiles = Path.Combine(path, "../res/skills");
	var skills = XDocument.Load(Path.Combine(path, "cards_config.xml")).Descendants("skillType")
	.Where(node => node.Element("icon") != null)
	.Select(node =>
	{
		var id = node.Element("id").Value;
		string icon;
		if (!skillIconChanges.TryGetValue(id, out icon))
		{
			if (!iconRemappings.TryGetValue(id, out icon))
			{
				icon = node.Element("icon").Value;
			}
		}
		string name;
		if (!skillRenames.TryGetValue(id, out name))
		{
			name = Clean(node.Element("name").Value);
		}
		skillIconNames.Add(icon);

		return new
		{
			id = id,
			name = name,
			desc = Clean(node.Element("desc").Value),
			icon = icon,
			type = (skillMappings.ContainsKey(id) ? skillMappings[id] : (node.Element("upkeep") != null ? "earlyActivation" : "activation")),
			order = node.Element("order").Value
		};
	})
	// Add missing skills
	.Union(new[] {
		new
		{
			id = "protect_seafolk",
			name = "Barrier",
			desc = "Reduces the next Damage dealt to a random allied creature",
			icon = "mystic_barrier",
			type = "activation",
			order = "0"
		}
	})
	.OrderBy(skill => skill.id);
	skills.GroupBy(skill => skill.icon).Where(g => g.Count() > 1).ToList().ForEach(g => g.Select(s => s.name).Dump(g.Key));

	skillIconNames.Where(icon => !File.Exists(Path.Combine(skillFiles, icon + ".png"))).ToList().Dump("Missing Icons");
	new DirectoryInfo(skillFiles).GetFiles("*.png", SearchOption.TopDirectoryOnly).ToList().Where(f => !skillIconNames.Contains(f.Name.Replace(".png", ""))).Select(f => f.Name).Dump("Extra Icon Files");

	var skillsJSON = String.Format("var SKILL_DATA = {{\r\n{0}\r\n}}", String.Join(",\r\n", skills.Select(skill => String.Format(
@"	{0} : {{
		name: '{1}',
		type: '{2}',
		icon: '{3}',
		desc: '{4}'
	}}", skill.id, skill.name, skill.type, skill.icon, skill.desc.Replace("'", "\'")))));
	File.WriteAllText(Path.Combine(path, "../scripts/data", "skills.js"), skillsJSON);

	var unusedImages = new DirectoryInfo(Path.Combine(path, @"..\res\cardImages"))
		.GetFiles("*.jpg")
		.Select(imageFile => imageFile.Name)
		.Where(name => name.IndexOf("SpriteSheet") < 0 && name.IndexOf("PortraitSheet") < 0 && name.IndexOf("NotFound") < 0)
		.ToDictionary(name => name.Replace(".jpg", ""), name => name);

	g_unitIDs = new HashSet<string>();

	var cardFiles = new[] 
	{ //"cards.xml"
		"cards_heroes.xml",
		"cards_premium_aether.xml",
		"cards_premium_chaos.xml",
		"cards_premium_wyld.xml",
		"cards_reward.xml",
		"cards_shard_cards.xml",
		"cards_special.xml",
		"cards_standard.xml",
		"cards_story.xml"
	};
	foreach (var filename in cardFiles)
	{
		xmlFile = Path.Combine(path, filename);
		var unitNodes = XDocument.Load(xmlFile).Descendants("unit");
		foreach (var unitXML in unitNodes)
		{
			var stringReader = new StringReader(unitXML.ToString());
			var unit = (unit)unitDeserializer.Deserialize(stringReader);
			units.Add(unit);
			if (!existingUnits.Contains(unit.id))
			{
				newUnits.Add(unit.id);
				unit.picture.Dump("New Image - " + unit.name);
			}
			if (unit.portrait != null)
			{
				pictures[unit.portrait] = unit.name;
				var imageFile = Path.Combine(path, @"..\res\cardImages\", unit.picture + ".png");
				if (!File.Exists(imageFile))
				{
					notFound[unit.picture] = unit.asset_bundle;
				}
			}
			else if (unit.picture != null)
			{
				pictures[unit.picture] = unit.name;
				var imageFile = Path.Combine(path, @"..\res\cardImages\", unit.picture + ".jpg");
				if (!File.Exists(imageFile))
				{
					notFound[unit.picture] = unit.asset_bundle;
				}
			}
			else
			{
				noImage.Add(unit.name + "(NO IMAGE)");
			}
			if (unusedImages.ContainsKey(unit.picture))
			{
				unusedImages.Remove(unit.picture);
			}
		}
	}

	notFound.Dump("Missing these");

	// Get Fusions
	xmlFile = Path.Combine(path, "fusion_recipes_cj2.xml");
	var doc = XDocument.Load(xmlFile);
	var fusions = doc.Descendants("fusion_recipe").Select(node => new fusionRecipe()
	{
		fusedCardID = node.Element("card_id").Value,
		baseCardID = node.Element("resource").Attribute("card_id").Value,
	});

	// Add placeholder units for unused images
	var idPrefixes = new[] { "", "1", "2" };
	var suffixMap = new Dictionary<char, int> { { 'A', 0 }, { 'B', 1 }, { 'C', 2} };
	var nameSuffixes = new[] { "S", "D", "Q" };
	var unitID = 10000;
	var unusedIDHash = new System.Collections.Generic.Dictionary<string, string>();
	var newFusions = new List<fusionRecipe>();
	foreach (var image in unusedImages)
	{
		var key = image.Key;
		if (key[0] != key.ToUpper()[0])
		{
			key.Dump();
			continue;
		}
		var split = key.LastIndexOf('_');
		if (split < 0)
		{
			key.Dump(); 
			continue;
		}
		var imageName = key.Substring(0, split);
		var suffix = image.Key[split + 1];
		int fusion = suffixMap[suffix];
		
		string fullID;
		if (unusedIDHash.ContainsKey(imageName))
		{
			fullID = idPrefixes[fusion] + unitID.ToString();
			newFusions.Add(new fusionRecipe()
			{
				baseCardID = unusedIDHash[imageName],
				fusedCardID = fullID
			});
			unusedIDHash[imageName] = fullID;
		}
		else
		{
			unitID--;
			fullID = idPrefixes[fusion] + unitID.ToString();
			unusedIDHash[imageName] = fullID;
		}
		
		var unit = new unit()
		{
			id = fullID,
			name = (imageName.IndexOf("New") == 0 ? "New Art" : String.Format("Unused Art - '{0}'", imageName)),
			picture = key,
			rarity = "0",
			card_type = "2",
			type = "0",
			attack = "-1",
			health = "-1",
			cost = "-1",
		};
		units.Add(unit);
		// Only add these to spoilers if there are other new units - don't want to overwrite spoilers with just new art
		/*
		if (newUnits.Count > 0)
		{
			newUnits.Add(unit.id);
		}
		*/
	}
	if (newFusions.Count > 0)
	{
		fusions = fusions.Union(newFusions);
	}

	if (newUnits.Count > 0 || (downloadFiles && forceSpoilers))
	{
		var spoilers = "var spoilers = {};\r\n" + String.Join("\r\n", newUnits.Select(id => String.Format("spoilers[{0}] = true;", id)));
		newUnits.Dump("New Units:");
		File.WriteAllText(Path.Combine(path, "../scripts/data", "spoilers.js"), spoilers);
	}

	// Get Locations
	var locations = XDocument.Load(Path.Combine(path, "missions.xml")).Descendants("location")
	.Select(node => new
	{
		id = node.Element("id").Value,
		name = node.Element("name").Value
	}).OrderBy(location => location.id)
	.Concat(new[] {
		new {
			id = "0",
			name = "Hero Upgrades"
		}
	});

	// Get Campaigns
	var campaigns = XDocument.Load(Path.Combine(path, "campaigns.xml")).Descendants("campaign")
	.Select(node => new campaign()
	{
		id = node.Element("id").Value,
		name = node.Element("name").Value,
		location_id = node.Element("location_id").Value,
		side_mission = (string)node.Element("side_mission"),
		battleground_id = (string)node.Element("battleground_id"),
		missions = node.Element("missions").Elements("mission_id").Select(mission_id => Int32.Parse(mission_id.Value)).ToArray(),
		items = node.Elements("find_item").Select(el => new item() {
			id = Int32.Parse(el.Attribute("id").Value),
			dropRate = Double.Parse(el.Attribute("drop_per_energy").Value)
		}).ToArray()
	}).OrderBy(campaign => campaign.location_id).ThenBy(campaign => campaign.id);

	// Get Missions
	var missions = missionsXML.Select(node => new mission()
	{
		id = node.Element("id").Value,
		name = node.Element("name").Value,
		commander = node.Elements("commander").Select(card => new missionCard()
		{
			id = card.Attribute("id").Value,
			level = (string)card.Attribute("level"),
		}).FirstOrDefault(),
		deck = node.Element("deck").Elements("card").Select(card => new missionCard()
		{
			id = card.Attribute("id").Value,
			level = (string)card.Attribute("level"),
			mastery_level = (string)card.Attribute("mastery_level"),
			remove_mastery_level = (string)card.Attribute("remove_mastery_level"),
		}).ToArray()
	}).OrderBy(mission => mission.id);

	var file = new FileInfo(Path.Combine(path, "../scripts/data", "cards.js"));
	using (var writer = file.CreateText())
	{
		writer.Write("var CARDS = {\r\n");
		foreach (var unit in units)
		{
			writer.Write(unit.ToString());
		}
		writer.Write("};\r\n");
		writer.Write("var CardsUpdated = " + DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds + ";");
	}

	file = new FileInfo(Path.Combine(path, "../scripts/data", "campaign.js"));
	using (var writer = file.CreateText())
	{
		writer.Write("var LOCATIONS = {\r\n");
		foreach (var location in locations)
		{
			writer.WriteLine("  \"" + location.id + "\": {");
			writer.WriteLine("    \"id\": \"" + location.id + "\",");
			writer.WriteLine("    \"name\": \"" + location.name + "\",");
			writer.WriteLine("  },");
		}
		writer.Write("};\r\n");

		writer.Write("var CAMPAIGNS = {\r\n");
		foreach (var campaign in campaigns)
		{
			writer.WriteLine("  \"" + campaign.id + "\": {");
			writer.WriteLine("    \"id\": \"" + campaign.id + "\",");
			writer.WriteLine("    \"name\": \"" + campaign.name + "\",");
			writer.WriteLine("    \"location_id\": \"" + campaign.location_id + "\",");
			if (!String.IsNullOrWhiteSpace(campaign.side_mission))
			{
				writer.WriteLine("    \"side_mission\": \"" + campaign.side_mission + "\",");
			}
			if (!String.IsNullOrWhiteSpace(campaign.battleground_id))
			{
				writer.WriteLine("    \"battleground_id\": \"" + campaign.battleground_id + "\",");
			}
			writer.WriteLine("    \"missions\": [\"" + String.Join("\",\"", campaign.missions) + "\"],");
			writer.WriteLine("    \"items\": {" + String.Join<item>(", ", campaign.items) + "}");
			writer.WriteLine("  },");
		}
		writer.Write("};\r\n");

		writer.WriteLine("var MISSIONS = {");
		foreach (var mission in missions)
		{
			writer.WriteLine("  \"" + mission.id + "\": {");
			writer.WriteLine("    \"id\": \"" + mission.id + "\",");
			writer.WriteLine("    \"name\": \"" + mission.name + "\",");
			writer.WriteLine("    \"commander\": {");
			writer.WriteLine(mission.commander.ToString());
			writer.WriteLine("    },");
			writer.WriteLine("    \"deck\": [");
			foreach (var card in mission.deck)
			{
				writer.WriteLine("      {");
				writer.WriteLine(card.ToString());
				writer.WriteLine("      },");
			}
			writer.WriteLine("    ]");
			writer.WriteLine("  },");
		}
		writer.WriteLine("};");
	}
	file = new FileInfo(Path.Combine(path, "../scripts/data", "fusions.js"));
	using (var writer = file.CreateText())
	{
		writer.WriteLine("var FUSIONS = {");
		writer.WriteLine(String.Join(",\r\n", fusions.OrderBy(f => f.baseCardID).Select(f => f.ToString())));
		writer.WriteLine("};");
		writer.WriteLine("var REVERSE_FUSIONS = {};");
		writer.WriteLine("for(var id in FUSIONS) {");
		writer.WriteLine("	var fusion = FUSIONS[id];");
		writer.WriteLine("	REVERSE_FUSIONS[fusion] = id;");
		writer.WriteLine("}");
	}
	file = new FileInfo(Path.Combine(path, "../scripts/data", "bges.js"));
	using (var writer = file.CreateText())
	{
		xmlFile = Path.Combine(path, "battleground_effects.xml");
		doc = XDocument.Load(xmlFile);

		var bgeNodes = doc.Descendants("battleground");
		var battlegrounds = new List<battleground>();
		foreach (var bgeXML in bgeNodes)
		{
			var stringReader = new StringReader(bgeXML.ToString());
			var bge = (battleground)bgeDeserializer.Deserialize(stringReader);
			// World Event BGEs
			if (Int32.Parse(bge.id) > 500)
			{
				if (bge.enemy_only)
				{
					bge.hidden = true;
				}
				else if (bge.effect.Where(effect => effect.id != "displayEffect").Count() == 0)
				{
					bge.hidden = true;
				}
				else if (Int32.Parse(bge.id) > 1000)
				{
					bge.hidden = true;
				}
				else
				{
					bge.hidden = false;
				}
			}
			battlegrounds.Add(bge);
		}
		writer.WriteLine("var BATTLEGROUNDS = {");
		for (int i = 0; i < battlegrounds.Count; i++)
		{
			var battleground = battlegrounds[i];
			writer.Write(battleground.ToString());
		}
		writer.WriteLine("};");
	}
}

private HashSet<string> LoadUnits(string file)
{
	var filePath = Path.Combine(path, file);
	var existingUnits = new HashSet<string>();
	if (File.Exists(filePath))
	{
		var doc = XDocument.Load(filePath);
		var unitNodes = doc.Descendants("unit");
		foreach (var unitXML in unitNodes)
		{
			var stringReader = new StringReader(unitXML.ToString());
			var unit = (unit)unitDeserializer.Deserialize(stringReader);
			existingUnits.Add(unit.id);
		}
	}
	return existingUnits;
}

public enum FactionIDs
{
	Aether = 1,
	Chaos = 2,
	Wyld = 3,
	Frog = 4,
	Elemental = 5,
	Angel = 6,
	Undead = 7,
	Void = 8,
	Dragon = 9,
	Avian = 10,
	Goblin = 11,
	Seafolk = 12,
	Insect = 13,
}

public class battleground
{
	private const string tabs = "    ";
	private const string tabs2 = "      ";
	private const string tabs3 = "        ";
	private const string tabs4 = "        ";

	public string name { get; set; }

	[XmlArrayItem(Type = typeof(add_skill), ElementName = "add_skill")]
	[XmlArrayItem(Type = typeof(evolve_skill), ElementName = "evolve_skill")]
	[XmlArrayItem(Type = typeof(skill), ElementName = "skill")]
	[XmlArrayItem(Type = typeof(scale_health), ElementName = "scale_health")]
	[XmlArrayItem(Type = typeof(scale_attributes), ElementName = "scale_attributes")]
	[XmlArrayItem(Type = typeof(on_play), ElementName = "on_play")]
	[XmlArrayItem(Type = typeof(starting_card), ElementName = "starting_card")]
	[XmlArrayItem(Type = typeof(trap_card), ElementName = "trap_card")]
	public battlegroundEffect[] effect { get; set; }
	public string id { get; set; }
	
	private string descField;
	public string desc
	{
		get { return this.descField; }
		set { this.descField = value.Replace("\"", "\\\""); }
	}

	[XmlIgnore]
	public bool enemy_only { get; set; }
	/// <summary>Get a value purely for serialization purposes</summary>
	[XmlElement("enemy_only")]
	public string enemy_onlySerialize
	{
		get { return this.enemy_only ? "1" : "0"; }
		set
		{
			if (value != null)
			{
				this.enemy_only = XmlConvert.ToBoolean(value);
			}
			else
			{
				this.enemy_only = false;
			}
		}
	}

	public string scale_with_level { get; set; }
	public string starting_level { get; set; }
	public bool hidden { get; set; }
	public bool isTower
	{
		get
		{
			return effect.Where(e => e is starting_card).Count() > 0;
		}
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder();
		sb.Append("  \"" + id + "\": {\r\n");
		sb.Append(tabs).Append("\"name\": \"").Append(name).Append("\",\r\n");
		sb.Append(tabs).Append("\"id\": \"").Append(id).Append("\",\r\n");
		sb.Append(tabs).Append("\"desc\": \"").Append(desc).Append("\",\r\n");
		if (enemy_only) sb.Append(tabs).Append("\"enemy_only\": ").Append(enemy_only.ToString().ToLower()).Append(",\r\n");
		if (scale_with_level != null) sb.Append(tabs).Append("\"scale_with_level\": \"").Append(scale_with_level).Append("\",\r\n");
		if (starting_level != null) sb.Append(tabs).Append("\"starting_level\": \"").Append(starting_level).Append("\",\r\n");
		if (hidden) sb.Append(tabs).Append("\"hidden\": ").Append(hidden.ToString().ToLower()).Append(",\r\n");
		if (isTower) sb.Append(tabs).Append("\"isTower\": ").Append(isTower.ToString().ToLower()).Append(",\r\n");
		AppendEffect(sb);
		sb.Append("  },\r\n");
		return sb.ToString();
	}

	private void AppendEffect(StringBuilder sb)
	{
		var closeTag = "]";
		var wroteSkills = false;
		for (int i = 0; i < effect.Length; i++)
		{
			var effect_i = effect[i];
			if (effect_i.skip) continue;
			if (effect_i is starting_card)
			{
				if (!wroteSkills)
				{
					wroteSkills = true;
					sb.Append(tabs).Append("\"effect\": {\r\n");
					closeTag = "}";
				}
				AppendTowerLevel(sb, (starting_card)effect_i, tabs3);
			}
			else
			{
				if (!wroteSkills)
				{
					wroteSkills = true;
					sb.Append(tabs).Append("\"effect\": [\r\n");
				}
				sb.Append(tabs2).Append("{\r\n");
				AppendEntryString(sb, "effect_type", effect_i.GetType().Name, tabs3);
				if (effect_i is evolve_skill)
				{
					AppendEvolve(sb, (evolve_skill)effect_i, tabs3);
				}
				else if (effect_i is add_skill)
				{
					AppendAddSkill(sb, (add_skill)effect_i, tabs3);
				}
				else if (effect_i is scale_health)
				{
					AppendScaleHealth(sb, (scale_health)effect_i, tabs3);
				}
				else if (effect_i is scale_attributes)
				{
					AppendScaling(sb, (scale_attributes)effect_i, tabs3);
				}
				else if (effect_i is on_play)
				{
					AppendOnPlay(sb, (on_play)effect_i, tabs3);
				}
				else if (effect_i is trap_card)
				{
					AppendTrap(sb, (trap_card)effect_i, tabs3);
				}
				else
				{
					AppendSkill(sb, (skill)effect_i, tabs3, false);
				}
				sb.Append(tabs2).Append("},\r\n");
			}
		}
		if (wroteSkills)
		{
			sb.Append(tabs).Append(closeTag + "\r\n");
		}
	}
}

public abstract class CardType
{
	public static string Hero = "1";
	public static string Unit = "2";
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
public class unit
{
	const string unitTabs = "    ";
	const string upgradeTabs = "      ";
	const string skillUpgradeDefTabs = "          ";
	const string skillUpgradePropTabs = "        ";
	const string skillTabs = "      ";
	const string skillUpgradeTabs = "        ";

	public void AppendUnit(StringBuilder sb)
	{
		if (name == "Croak Dropper")
		{
			var breakpoint = true;
		}
		
		sb.Append("  \"").Append(id).Append("\": {\r\n");
		AppendEntryString(sb, "id", id, unitTabs);
		AppendEntryString(sb, "name", name, unitTabs);
		AppendEntryString(sb, "desc", desc, unitTabs);
		AppendEntryString(sb, "picture", picture, unitTabs);
		AppendEntryString(sb, "hidden_until", hidden_until, unitTabs);
		AppendEntryString(sb, "rarity", rarity, unitTabs);
		AppendEntryString(sb, "set", set, unitTabs);
		AppendEntryString(sb, "card_type", card_type, unitTabs);
		AppendEntry(sb, "shard_card", shard_card, unitTabs);
		AppendEntryString(sb, "type", type, unitTabs);
		AppendEntryArray(sb, "sub_type", sub_type, unitTabs);
		AppendEntry(sb, "health", health, unitTabs);
		if (card_type != "1")
		{
			AppendEntry(sb, "attack", attack, unitTabs);
			AppendEntry(sb, "cost", cost, unitTabs);
		}
		
		var maxLevel = 1 + (upgrades != null ? upgrades.Length : 0);
		AppendEntry(sb, "maxLevel", maxLevel.ToString(), unitTabs);
		
		AppendSkills(sb, skills, unitTabs);
		AppendUpgrades(sb);
		sb.Append("  },\r\n");
	}

	public override string ToString()
	{
		StringBuilder unit = new StringBuilder();
		AppendUnit(unit);
		if (!g_unitIDs.Add(this.id))
		{
			"Conflict".Dump(this.id + " : " + this.name);
		}
		return unit.ToString();
	}

	private void AppendUpgrades(StringBuilder sb)
	{
		if (upgrades == null || upgrades.Length == 0)
		{
			sb.Append(unitTabs).Append("\"upgrades\": {}\r\n");
		}
		else
		{
			sb.Append(unitTabs).Append("\"upgrades\": {\r\n");
			foreach (var upgrade in upgrades)
			{
				sb.Append(upgradeTabs).Append("\"").Append(upgrade.level).Append("\": {\r\n");
				AppendEntry(sb, "attack", upgrade.attack, skillUpgradePropTabs);
				AppendEntry(sb, "health", upgrade.health, skillUpgradePropTabs);
				AppendEntry(sb, "cost", upgrade.cost, skillUpgradePropTabs);
				AppendEntryString(sb, "desc", upgrade.desc, skillUpgradePropTabs);
				AppendSkills(sb, upgrade.skills, skillUpgradePropTabs);
				sb.Append(upgradeTabs).Append("},\r\n");
			}
			sb.Append(unitTabs).Append("}\r\n");
		}
	}

	private string idField;
	private string card_typeField;
	private string shard_cardField;
	private string nameField;
	private string descField;
	private string pictureField;
	private string portraitField;
	private string asset_prefabField;
	private string asset_bundleField;
	private string hidden_untilField;
	private string attackField;
	private string healthField;
	private string costField;
	private string rarityField;
	private string typeField;
	private string[] sub_typeField;
	private string setField;
	private skill[] skillsField;
	private unitUpgrade[] upgradesField;

	/// <remarks/>
	public string id
	{
		get { return this.idField; }
		set { this.idField = value; }
	}

	/// <remarks/>
	public string card_type
	{
		get { return this.card_typeField ?? "2"; }
		set { this.card_typeField = value; }
	}

	/// <remarks/>
	public string shard_card
	{
		get { return this.shard_cardField; }
		set { this.shard_cardField = value; }
	}

	/// <remarks/>
	public string name
	{
		get { return this.nameField; }
		set { this.nameField = value; }
	}

	/// <remarks/>
	public string desc
	{
		get { return this.descField; }
		set { this.descField = value.Replace("\"", "\\\""); }
	}

	/// <remarks/>
	public string picture
	{
		get { return this.pictureField ?? this.portrait ?? this.asset_prefab; }
		set { this.pictureField = value; }
	}

	public string asset_prefab
	{
		get
		{
			if (String.IsNullOrWhiteSpace(this.asset_prefabField))
			{
				return String.Empty;
			}
			else
			{
				return "prefab_" + this.asset_prefabField;
			}
		}
		set { this.asset_prefabField = value; }
	}

	/// <remarks/>
	public string portrait
	{
		get
		{
			if (this.portraitField != null)
			{
				return "portrait_" + this.portraitField.ToLower().Replace("portrait_", "").Replace("Portrait_", "");
			}
			else
			{
				return null;
			}
		}
		set { this.portraitField = value; }
	}

	/// <remarks/>
	public string asset_bundle
	{
		get { return this.asset_bundleField; }
		set { this.asset_bundleField = value; }
	}

	/// <remarks/>
	public string hidden_until
	{
		get { return this.hidden_untilField; }
		set
		{
			if (!String.IsNullOrEmpty(value))
			{
				this.hidden_untilField = value + "000";
			}
		}
	}
	
	public string hidden_until_time
	{
		get { return this.hidden_untilField; }
		set
		{
			if (!String.IsNullOrEmpty(value))
			{
				this.hidden_untilField = value + "000";
			}
		}
	}

	/// <remarks/>
	public string attack
	{
		get { return this.attackField; }
		set { this.attackField = value; }
	}

	/// <remarks/>
	public string health
	{
		get { return this.healthField; }
		set { this.healthField = value; }
	}

	/// <remarks/>
	public string cost
	{
		get { return this.costField; }
		set { this.costField = value; }
	}

	/// <remarks/>
	public string rarity
	{
		get { return this.rarityField; }
		set { this.rarityField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlElementAttribute("skill")]
	public skill[] skills
	{
		get { return this.skillsField; }
		set { this.skillsField = value; }
	}

	/// <remarks/>
	public string type
	{
		get { return this.typeField; }
		set { this.typeField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlElementAttribute("sub_type")]
	public string[] sub_type
	{
		get { return this.sub_typeField; }
		set { this.sub_typeField = value; }
	}

	/// <remarks/>
	public string set
	{
		get { return this.setField; }
		set { this.setField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlElementAttribute("upgrade")]
	public unitUpgrade[] upgrades
	{
		get { return this.upgradesField; }
		set { this.upgradesField = value; }
	}
}
/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public class unitUpgrade
{
	private string levelField;
	private string attackField;
	private string healthField;
	private string costField;
	private string descField;
	private skill[] skillField;

	/// <remarks/>
	public string level
	{
		get { return this.levelField; }
		set { this.levelField = value; }
	}

	/// <remarks/>
	public string attack
	{
		get { return this.attackField; }
		set { this.attackField = value; }
	}

	/// <remarks/>
	public string health
	{
		get { return this.healthField; }
		set { this.healthField = value; }
	}

	/// <remarks/>
	public string cost
	{
		get { return this.costField; }
		set { this.costField = value; }
	}

	public string desc
	{
		get { return this.descField; }
		set { this.descField = value.Replace("\"", "\\\""); }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlElementAttribute("skill")]
	public skill[] skills
	{
		get { return this.skillField; }
		set { this.skillField = value; }
	}
}

/// <remarks/>
public abstract class battlegroundEffect
{
	private string yField;
	private string idField;

	public virtual bool skip { get { return false; } }

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string id
	{
		get { return this.idField; }
		set { this.idField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string y
	{
		get { return this.yField; }
		set { this.yField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string yy
	{
		get { return this.yField; }
		set { this.yField = value; }
	}
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public class skill : battlegroundEffect
{
	private string xField;
	private string cField;
	private string sField;
	private string allField;
	private string multField;
	private string on_delay_multField;

	public override bool skip
	{
		get
		{
			if (id == "displayEffect")
			{
				return true;
			}
			return false;
		}
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string x
	{
		get { return this.xField; }
		set { this.xField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string mult
	{
		get { return this.multField; }
		set { this.multField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string on_delay_mult
	{
		get { return this.on_delay_multField; }
		set { this.on_delay_multField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string c
	{
		get { return this.cField; }
		set { this.cField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string s
	{
		get { return this.sField; }
		set { this.sField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string all
	{
		get { return this.allField; }
		set { this.allField = value; }
	}
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public class evolve_skill : battlegroundEffect
{
	private string sField;
	private string allField;

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string s
	{
		get { return this.sField; }
		set { this.sField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string all
	{
		get { return this.allField; }
		set { this.allField = value; }
	}
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public class add_skill : skill
{
	private string baseField;
	private string cardField;
	private string levelField;
	private string rarityField;

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute("base")]
	public string Base
	{
		get { return this.baseField; }
		set { this.baseField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string rarity
	{
		get { return this.rarityField; }
		set { this.rarityField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string card
	{
		get { return this.cardField; }
		set { this.cardField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string level
	{
		get { return this.levelField; }
		set { this.levelField = value; }
	}
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public class scale_attributes : battlegroundEffect
{
	private string base_multField;
	private string multField;

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string base_mult
	{
		get { return this.base_multField; }
		set { this.base_multField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string mult
	{
		get { return this.multField; }
		set { this.multField = value; }
	}
}

[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public class scale_health : scale_attributes
{
	private string baseField;

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute("base")]
	public string Base
	{
		get { return this.baseField; }
		set { this.baseField = value; }
	}
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public class on_play : battlegroundEffect
{
	private string attackerField;
	private string defenderField;
	private string first_playField;

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string attacker
	{
		get { return this.attackerField; }
		set { this.attackerField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string defender
	{
		get { return this.defenderField; }
		set { this.defenderField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string first_play
	{
		get { return this.first_playField; }
		set { this.first_playField = value; }
	}

	[XmlElement("add_skill", typeof(add_skill))]
	[XmlElement("evolve_skill", typeof(evolve_skill))]
	[XmlElement("scale_health", typeof(scale_health))]
	[XmlElement("scale_attributes", typeof(scale_attributes))]
	[XmlElement("skill", typeof(skill))]
	public battlegroundEffect effect { get; set; }
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public class starting_card : battlegroundEffect
{
	private string levelField;
	private string rankField;

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string level
	{
		get { return this.levelField; }
		set { this.levelField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string pvp_rank
	{
		get { return this.rankField; }
		set { this.rankField = value; }
	}
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public class trap_card : battlegroundEffect
{
	private string baseField;
	private string multField;
	private string target_deckField;

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string mult
	{
		get { return this.multField; }
		set { this.multField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute("base")]
	public string Base
	{
		get { return this.baseField; }
		set { this.baseField = value; }
	}

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string target_deck
	{
		get { return this.target_deckField; }
		set { this.target_deckField = value; }
	}
}

public class campaign
{
	public string id { get; set; }
	public string name { get; set; }
	public string location_id { get; set; }
	public string side_mission { get; set; }
	public string battleground_id { get; set;}
	public int[] missions { get; set; }
	public item[] items {get; set; }
}

public class item {
	public int id;
	public double dropRate;

	public override string ToString()
	{
		return String.Format("\"{0}\": {1}", id, dropRate);
	}
}

public class mission
{
	public string id { get; set; }
	public string name { get; set; }
	public missionCard commander { get; set; }
	public missionCard[] deck { get; set; }
}

public class missionCard
{
	public string id { get; set; }
	public string level { get; set; }
	public string mastery_level { get; set; }
	public string remove_mastery_level { get; set; }

	public override string ToString()
	{
		var fields = new List<string>();
		var spaces = "        ";
		AddFieldIfSpecified(fields, "id", id, spaces);
		AddFieldIfSpecified(fields, "level", level, spaces);
		AddFieldIfSpecified(fields, "mastery_level", mastery_level, spaces);
		AddFieldIfSpecified(fields, "remove_mastery_level", remove_mastery_level, spaces);
		return String.Join(",\r\n", fields);
	}
}

private static void AddFieldIfSpecified(List<string> fields, string name, string value, string spaces)
{
	if (!String.IsNullOrWhiteSpace(value))
	{
		fields.Add(spaces + "\"" + name + "\": \"" + value + "\"");
	}
}

public partial class fusionRecipe
{
	public string baseCardID;
	public string fusedCardID;

	public override string ToString()
	{
		return "  \"" + baseCardID + "\" : \"" + fusedCardID + "\"";
	}
}

private static void AppendEntry(StringBuilder sb, string name, string value, string tabs)
{
	if (value != null)
	{
		sb.Append(tabs).Append("\"").Append(name).Append("\": ").Append(value).Append(",\r\n");
	}
}

private static void AppendEntryString(StringBuilder sb, string name, string value, string tabs, string defaultValue = null)
{
	if (!String.IsNullOrEmpty(value))
	{
		sb.Append(tabs).Append("\"").Append(name).Append("\": \"").Append(value).Append("\",\r\n");
	}
	else if (!String.IsNullOrEmpty(defaultValue))
	{
		sb.Append(tabs).Append("\"").Append(name).Append("\": \"").Append(defaultValue).Append("\",\r\n");
	}
}

private static void AppendEntryArray(StringBuilder sb, string name, string[] values, string tabs)
{
	sb.Append(tabs).Append("\"").Append(name).Append("\": [");
	if (values != null && values.Length > 0)
	{
		sb.Append("\"").Append(String.Join("\",\"", values)).Append("\"");
	}
	sb.Append("],\r\n");
}

private static void AppendSkills(StringBuilder sb, skill[] skills, string tabs)
{
	if (skills == null || skills.Length == 0)
	{
		//sb.Append(tabs).Append("\"skill\": {},\r\n");
		sb.Append(tabs).Append("\"skill\": [],\r\n");
	}
	else
	{
		string skillDefTabs = tabs + "  ";
		string skillPropTabs = skillDefTabs + "  ";
		//sb.Append(tabs).Append("\"skill\": {\r\n");
		sb.Append(tabs).Append("\"skill\": [\r\n");
		foreach (var skill in skills)
		{
			AppendSkill(sb, skill, skillDefTabs);
		}
		//sb.Append(tabs).Append("},\r\n");
		sb.Append(tabs).Append("],\r\n");
	}
}

private static void AppendSkill(StringBuilder sb, skill skill, string tabs, bool braces = true)
{
	var propTabs = tabs + (braces ? "  " : "");
	//sb.Append(tabs).Append("\"").Append(skill.id).Append("\": {\r\n");
	if (braces)
	{
		sb.Append(tabs).Append("{\r\n");
	}

	AppendEntryString(sb, "id", skill.id, propTabs);
	AppendEntry(sb, "x", skill.x, propTabs);
	AppendEntry(sb, "mult", skill.mult, propTabs);
	AppendEntry(sb, "on_delay_mult", skill.on_delay_mult, propTabs);
	AppendEntryString(sb, "y", skill.y, propTabs);
	//AppendEntry(sb, "z", skill.z, propTabs);
	AppendEntry(sb, "c", skill.c, propTabs);
	AppendEntryString(sb, "s", skill.s, propTabs);
	AppendEntryString(sb, "all", skill.all, propTabs);

	if (braces)
	{
		sb.Append(tabs).Append("},\r\n");
	}
}

private static void AppendEvolve(StringBuilder sb, evolve_skill evolve, string tabs)
{
	AppendEntryString(sb, "id", evolve.id, tabs);
	AppendEntryString(sb, "s", evolve.s, tabs);
	AppendEntryString(sb, "all", evolve.all, tabs);
}

private static void AppendAddSkill(StringBuilder sb, add_skill skill, string tabs)
{
	AppendEntryString(sb, "id", skill.id, tabs);
	AppendEntry(sb, "x", skill.x, tabs);
	AppendEntry(sb, "mult", skill.mult, tabs);
	AppendEntry(sb, "on_delay_mult", skill.on_delay_mult, tabs);
	AppendEntryString(sb, "base", skill.Base, tabs);
	AppendEntryString(sb, "y", skill.y, tabs);
	//AppendEntry(sb, "z", skill.z, tabs);
	AppendEntry(sb, "c", skill.c, tabs);
	AppendEntryString(sb, "s", skill.s, tabs);
	AppendEntryString(sb, "all", skill.all, tabs);
	AppendEntry(sb, "rarity", skill.rarity, tabs);
	AppendEntry(sb, "card", skill.card, tabs);
	AppendEntry(sb, "level", skill.level, tabs);
}

private static void AppendScaling(StringBuilder sb, scale_attributes skill, string tabs)
{
	AppendEntryString(sb, "id", skill.id, tabs);
	AppendEntry(sb, "base_mult", skill.base_mult, tabs);
	AppendEntry(sb, "mult", skill.mult, tabs);
	AppendEntryString(sb, "y", skill.y, tabs);
}

private static void AppendScaleHealth(StringBuilder sb, scale_health skill, string tabs)
{
	AppendEntryString(sb, "id", skill.id, tabs);
	AppendEntryString(sb, "base", skill.Base, tabs);
	AppendEntry(sb, "base_mult", skill.base_mult, tabs);
	AppendEntry(sb, "mult", skill.mult, tabs);
	AppendEntryString(sb, "y", skill.y, tabs);
}

private static void AppendOnPlay(StringBuilder sb, on_play skill, string tabs)
{
	AppendEntryString(sb, "id", skill.id, tabs);
	AppendEntry(sb, "attacker", skill.attacker, tabs);
	AppendEntry(sb, "defender", skill.defender, tabs);
	AppendEntry(sb, "first_play", skill.first_play, tabs);

	sb.Append(tabs).Append("\"effect\": {\r\n");
	var effect = skill.effect;
	var tabs2 = tabs + "\t";
	AppendEntryString(sb, "effect_type", effect.GetType().Name, tabs2);
	if (effect is evolve_skill)
	{
		AppendEvolve(sb, (evolve_skill)effect, tabs2);
	}
	else if (effect is add_skill)
	{
		AppendAddSkill(sb, (add_skill)effect, tabs2);
	}
	else if (effect is scale_health)
	{
		AppendScaleHealth(sb, (scale_health)effect, tabs2);
	}
	else if (effect is scale_attributes)
	{
		AppendScaling(sb, (scale_attributes)effect, tabs2);
	}
	else if (effect is on_play)
	{
		AppendOnPlay(sb, (on_play)effect, tabs2);
	}
	else if (effect is trap_card)
	{
		AppendTrap(sb, (trap_card)effect, tabs2);
	}
	else
	{
		AppendSkill(sb, (skill)effect, tabs2, false);
	}
	sb.Append(tabs).Append("}\r\n");
}

private static void AppendTowerLevel(StringBuilder sb, starting_card info, string tabs)
{
	sb.Append(tabs).Append("\"").Append(info.pvp_rank).Append("\"").Append(": ").Append("{\r\n");
	var tabs2 = tabs + "\t";
	AppendEntry(sb, "id", info.id, tabs2);
	AppendEntry(sb, "level", info.level, tabs2);
	sb.Append(tabs).Append("},\r\n");
}

private static void AppendTrap(StringBuilder sb, trap_card info, string tabs)
{
	AppendEntry(sb, "id", info.id, tabs);
	AppendEntryString(sb, "base", info.Base, tabs);
	AppendEntry(sb, "mult", info.mult, tabs);
	AppendEntryString(sb, "target_deck", info.target_deck, tabs);
	AppendEntryString(sb, "y", info.y, tabs);
}

private static void Normalize(string fileName, bool downloadFiles)
{
	string filepath = Path.Combine(path, fileName);
	string url = Path.Combine(baseUrl, fileName);
	if (downloadFiles)
	{
		webClient.DownloadFile(url, filepath);
	}
	XDocument.Load(filepath).Save(filepath);
}

private static string Clean(string input)
{
	if (input != null)
	{
		return input.Replace(@"\", @"\\").Replace("'", @"\'");
	}
	else
	{
		return input;
	}
}