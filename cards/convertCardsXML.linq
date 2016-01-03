<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.XML.dll</Reference>
</Query>

static bool getImages = false;
static string path = @"D:\Programs\Source\Repos\SIMSpellstone\cards";
static string baseUrl = @"https://spellstone.synapse-games.com/assets";


static System.Net.WebClient webClient = new System.Net.WebClient();
static HashSet<string> g_unitIDs;

void Main()
{
	Normalize(Path.Combine("cards.xml"));
	Normalize(Path.Combine("missions.xml"));
	Normalize(Path.Combine("fusion_recipes_cj2.xml"));
	Normalize(Path.Combine("levels.xml"));
	
	g_unitIDs = new HashSet<string>();
	var xmlFile = Path.Combine(path, "cards.xml");
	var doc = XDocument.Load(xmlFile);
	
	System.Xml.Serialization.XmlSerializer unitDeserializer = new System.Xml.Serialization.XmlSerializer(typeof(unit));

	StringBuilder sbJSON = new StringBuilder();
	List<unit> units = new List<unit>();

	var pictures = new Dictionary<string, string>();
	var notFound = new List<string>();

	var unitNodes = doc.Descendants("unit");
	foreach (var unitXML in unitNodes)
	{
		var stringReader = new StringReader(unitXML.ToString());
		var unit = (unit)unitDeserializer.Deserialize(stringReader);
		units.Add(unit);
		if (unit.picture != null)
		{
			pictures[unit.picture] = unit.name;
			var imageFile = Path.Combine(path, @"..\res\cardImages\", unit.picture+".jpg");
			if(!File.Exists(imageFile))
			{
				unit.picture = "NotFound";
			}
		}
		else
		{
			notFound.Add(unit.name + "(NO IMAGE)");
		}
	}

	xmlFile = Path.Combine(path, "missions.xml");
	doc = XDocument.Load(xmlFile);
	var missions = doc.Descendants("mission").Select(node => new mission()
	{
		id = node.Element("id").Value,
		name = node.Element("name").Value,
		commander = node.Element("commander").Attribute("id").Value,
		deck = node.Element("deck").Elements("card").Where(card => card.Attribute("remove_mastery_level") == null).Select(card => card.Attribute("id").Value).ToArray()
	}).OrderBy(m => m.id);

	xmlFile = Path.Combine(path, "fusion_recipes_cj2.xml");
	doc = XDocument.Load(xmlFile);
	var fusions = doc.Descendants("mission").Select(node => new mission()
	{
		id = node.Element("id").Value,
		name = node.Element("name").Value,
		commander = node.Element("commander").Attribute("id").Value,
		deck = node.Element("deck").Elements("card").Select(card => card.Attribute("id").Value).ToArray()
	}).OrderBy(m => m.id);

	var file = new FileInfo(Path.Combine(path, "cache.js"));
	using (var writer = file.CreateText())
	{
		writer.Write("var CARDS = {\r\n");
		writer.Write("\"root\": {\r\n");
		writer.Write("\"unit\": {\r\n");
		foreach (var unit in units)
		{
			writer.Write(unit.ToString());
		}
		writer.Write("}\r\n");
		writer.Write("}\r\n");
		writer.Write("};\r\n");
		writer.WriteLine("var missions = {");
		writer.WriteLine("  root: {");
		writer.WriteLine("    mission: {");
		foreach (var mission in missions)
		{
			writer.WriteLine("      \"" + mission.id + "\": {");
			writer.WriteLine("        \"id\": \"" + mission.id + "\",");
			writer.WriteLine("        \"name\": \"" + mission.name + "\",");
			writer.WriteLine("        \"commander\": \"" + mission.commander + "\",");
			writer.WriteLine("        \"deck\": [");
			foreach (var card in mission.deck)
			{
				writer.WriteLine("          \"" + card + "\",");
			}
			writer.WriteLine("        ]");
			writer.WriteLine("      },");
		}
		writer.WriteLine("    }");
		writer.WriteLine("  }");
		writer.WriteLine("};");

		writer.WriteLine("var achievements = [];");
		writer.WriteLine("var raids = [];");
		writer.WriteLine("var quests = {");
		writer.WriteLine("  root: {");
		writer.WriteLine("    battleground: [");
		for (int i = 0; i < battlegrounds.Length; i++)
		{
			var battleground = battlegrounds[i];
			battleground.ID = i.ToString();
			writer.Write(battleground.ToString());
		}
		writer.WriteLine("    ]");
		writer.WriteLine("  }");
		writer.WriteLine("};");
	}

	if(!getImages) return;
	
	var baseurl = @"http://spellstone.wikia.com/wiki/File:";
	var folder = @"C:\Users\jsen\Documents\Visual Studio 2013\Projects\SIMSpellstone\res\cardImages\";
	//pictures.Dump("Image URLs");
	var client = new System.Net.WebClient();
	foreach (var name in pictures.Keys)
	{
		var url1 = baseurl + name + ".png";
		var file1 = folder + name + ".png";
		var url2 = baseurl + name + ".jpg";
		var file2 = folder + name + ".jpg";
		if (!File.Exists(file1))
		{
			if (name.EndsWith("_A") || name.EndsWith("_B") || name.EndsWith("_C"))
			{
				notFound.Add(name);
				continue;
			}
			name.Dump();
			try
			{
				var url = "http://spellstone.wikia.com/wiki/File:Portrait_" + name + ".png";
				var html = new FileInfo(folder + name + ".html");
				client.DownloadFile(url, html.FullName);
				string contents;
				using (var reader = html.OpenText())
				{
					contents = reader.ReadToEnd();
				}
				var regex = new Regex("<meta property=\"og:image\" content=\"(.*" + name + ".*)\".*/>");
				var matches = regex.Match(contents);
				if (matches.Success)
				{
					html.Delete();
					url = matches.Groups[1].Value;
					var extension = (url.Contains(".jpg") ? ".jpg" : ".png");
					client.DownloadFile(url, @"C:\Users\jsen\Documents\Visual Studio 2013\Projects\SIMSpellstone\res\cardImages\" + name + extension);
				}
				else
				{
					regex = new Regex("<meta property=\"og:image\" content=\"(http://vignette1.wikia.nocookie.net/spellstone/images.*)\".*/>");
					matches = regex.Match(contents);
					if (matches.Success)
					{
						html.Delete();
						url = matches.Groups[1].Value;
						var extension = (url.Contains(".jpg") ? ".jpg" : ".png");
						client.DownloadFile(url, @"C:\Users\jsen\Documents\Visual Studio 2013\Projects\SIMSpellstone\res\cardImages\" + name + extension);
					}
					else
					{
					"Fail".Dump();
					}
				}
				//client.DownloadFile(url1, file1);
			}
			catch
			{
				try
				{
					client.DownloadFile(url1, file1);
				}
				catch
				{
					notFound.Add(name + " : " + pictures[name]);
				}
			}
		}
	}
	notFound.Dump("Not Found");
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
	Dragon = 9
}

battleground[] battlegrounds = new battleground[] {
	new battleground {
		Name = "Rise of the Frogs",
		Effect = new skill() {
			id = "protect",
			x = "2",
			y = ((int)FactionIDs.Frog).ToString(),
			all = "1",
		},
	},
	new battleground {
		Name = "World Awakening",
		Effect = new skill() {
			id = "rally",
			x = "2",
			y = ((int)FactionIDs.Elemental).ToString(),
			all = "1",
		},
	},
	new battleground {
		Name = "Age of the Dragons",
		Effect = new skill() {
			id = "heal",
			//x = "2",
			mult = "0.2",
            y = ((int)FactionIDs.Dragon).ToString(),
			all = "1",
		},
	},
};

public class battleground
{
	private const string tabs = "        ";
	private const string tabs2 = "          ";
	
	public string Name { get; set; }
	public skill Effect { get; set; }
	public string ID { get; set; }

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder();
		sb.Append("      {\r\n");
		sb.Append(tabs).Append("\"name\": \"").Append(Name).Append("\",\r\n");
		sb.Append(tabs).Append("\"id\": \"").Append(ID).Append("\",\r\n");
		sb.Append(tabs).Append("\"skill\": [\r\n");
		AppendSkill(sb, Effect, tabs2);
		sb.Append(tabs).Append("]\r\n");
		sb.Append("      },\r\n");
		return sb.ToString();
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
public partial class unit
{
	const string unitTabs = "    ";
	const string upgradeTabs = "      ";
	const string skillUpgradeDefTabs = "          ";
	const string skillUpgradePropTabs = "        ";
	const string skillTabs = "      ";
	const string skillUpgradeTabs = "        ";

	public void AppendUnit(StringBuilder sb)
	{
		sb.Append("  \"").Append(id).Append("\": {\r\n");
		AppendEntryString(sb, "id", id, unitTabs);
		AppendEntryString(sb, "name", name, unitTabs);
		AppendEntryString(sb, "picture", picture, unitTabs);
		AppendEntryString(sb, "rarity", rarity, unitTabs);
		AppendEntryString(sb, "set", set, unitTabs);
		AppendEntryString(sb, "card_type", card_type, unitTabs);
		AppendEntryString(sb, "type", type, unitTabs);
		AppendEntryString(sb, "sub_type", sub_type, unitTabs);
		AppendEntry(sb, "attack", attack, unitTabs);
		AppendEntry(sb, "health", health, unitTabs);
		AppendEntry(sb, "cost", cost, unitTabs);
		AppendSkills(sb, skills, unitTabs);
		AppendUpgrades(sb);
		sb.Append("  },\r\n");
	}

	public override string ToString()
	{
		StringBuilder unit = new StringBuilder();
		AppendUnit(unit);
		if(!g_unitIDs.Add(this.id)) {
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
				AppendSkills(sb, upgrade.skills, skillUpgradePropTabs);
				sb.Append(upgradeTabs).Append("},\r\n");
			}
			sb.Append(unitTabs).Append("}\r\n");
		}
	}

	private string idField;
	private string card_typeField;
	private string nameField;
	private string pictureField;
	private string portraitField;
	private string asset_prefabField;
	private string asset_bundleField;
	private string attackField;
	private string healthField;
	private string costField;
	private string rarityField;
	private string typeField;
	private string sub_typeField;
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
		get { return this.card_typeField; }
		set { this.card_typeField = value; }
	}

	/// <remarks/>
	public string name
	{
		get { return this.nameField; }
		set { this.nameField = value; }
	}

	/// <remarks/>
	public string picture
	{
		get { return this.pictureField ?? this.portraitField ?? this.asset_prefabField; }
		set { this.pictureField = value; }
	}

	public string asset_prefab
	{
		get { return this.asset_prefabField; }
		set { this.asset_prefabField = value; }
	}

	/// <remarks/>
	public string portrait
	{
		get { return this.portraitField; }
		set { this.portraitField = value; }
	}

	/// <remarks/>
	public string asset_bundle
	{
		get { return this.asset_bundleField; }
		set { this.asset_bundleField = value; }
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
	public string sub_type
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
public partial class unitUpgrade
{
	private string levelField;
	private string attackField;
	private string healthField;
	private string costField;
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

	/// <remarks/>
	[System.Xml.Serialization.XmlElementAttribute("skill")]
	public skill[] skills
	{
		get { return this.skillField; }
		set { this.skillField = value; }
	}
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class skill
{
	private string idField;
	private string xField;
	private string yField;
	private string cField;
	private string sField;
	private string allField;
	private string multField;

	/// <remarks/>
	[System.Xml.Serialization.XmlAttributeAttribute()]
	public string id
	{
		get { return this.idField; }
		set { this.idField = value; }
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
	public string y
	{
		get { return this.yField; }
		set { this.yField = value; }
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

public partial class mission
{
	public string id { get; set; }
	public string name { get; set; }
	public string commander { get; set; }
	public string[] deck { get; set; }
}

private static void AppendEntry(StringBuilder sb, string name, string value, string tabs)
{
	if (value != null)
	{
		sb.Append(tabs).Append("\"").Append(name).Append("\": ").Append(value).Append(",\r\n");
	}
}

private static void AppendEntryString(StringBuilder sb, string name, string value, string tabs)
{
	if (value != null)
	{
		sb.Append(tabs).Append("\"").Append(name).Append("\"").Append(": ").Append("\"").Append(value).Append("\",\r\n");
	}
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

private static void AppendSkill(StringBuilder sb, skill skill, string tabs)
{
	var propTabs = tabs + "  ";
	//sb.Append(tabs).Append("\"").Append(skill.id).Append("\": {\r\n");
	sb.Append(tabs).Append("{\r\n");

	AppendEntryString(sb, "id", skill.id, propTabs);
	AppendEntry(sb, "x", skill.x, propTabs);
	AppendEntry(sb, "mult", skill.mult, propTabs);
	AppendEntryString(sb, "y", skill.y, propTabs);
	AppendEntry(sb, "z", skill.y, propTabs);
	AppendEntry(sb, "c", skill.c, propTabs);
	AppendEntryString(sb, "s", skill.s, propTabs);
	AppendEntryString(sb, "all", skill.all, propTabs);
	
	sb.Append(tabs).Append("},\r\n");
}

private static void Normalize(string fileName)
{
	string filepath = Path.Combine(path, fileName);
	string url = Path.Combine(baseUrl, fileName);
	webClient.DownloadFile(url, filepath);
	XDocument.Load(filepath).Save(filepath);
}