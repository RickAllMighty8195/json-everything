using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using GenerateSponsorList;
using Microsoft.Extensions.DependencyInjection;
using JsonSerializer = System.Text.Json.JsonSerializer;

/*  This allows for manually capturing the GQL response via wireshark and applying any data fixes
//  See also https://github.com/ChilliCream/graphql-platform/issues/7207
var filename = @"C:\Users\gregs\Downloads\gql-response.json";
/*/
var filename = args.Length == 0 ? null : args[0];
//*/

List<SponsorData> sponsorData;
if (filename is null)
{
	var accessToken = Environment.GetEnvironmentVariable("GENERATE_SPONSOR_LIST");
	if (string.IsNullOrEmpty(accessToken))
		throw new ArgumentException("Cannot locate GitHub GraphQL API access token in env var `GENERATE_SPONSOR_LIST`");

	var serviceCollection = new ServiceCollection();

	serviceCollection
		.AddGitHubClient()
		.ConfigureHttpClient(client =>
		{
			client.BaseAddress = new Uri("https://api.github.com/graphql");
			client.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", accessToken);
		});

	IServiceProvider services = serviceCollection.BuildServiceProvider();

	var client = services.GetRequiredService<IGitHubClient>();
	var response = await client.GetSponsors.ExecuteAsync();

	sponsorData = new List<SponsorData>();
	var seenSponsors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	foreach (var node in response.Data!.User!.SponsorshipsAsMaintainer.Nodes!)
	{
		string? name = null;
		Uri? avatar = null;
		Uri? website = null;
		var value = node!.Tier!.MonthlyPriceInDollars;
		
		if (node!.SponsorEntity is IGetSponsors_User_SponsorshipsAsMaintainer_Nodes_SponsorEntity_User { Login: not null } user)
		{
			name = user.Login;
			avatar = user.AvatarUrl;
			website = user.WebsiteUrl;
		}
		else if (node.SponsorEntity is IGetSponsors_User_SponsorshipsAsMaintainer_Nodes_SponsorEntity_Organization { Login: not null } org)
		{
			name = org.Login;
			avatar = org.AvatarUrl;
			website = org.WebsiteUrl;
		}

		if (name is null || avatar is null) continue;
		if (!seenSponsors.Add(name)) continue;

		sponsorData.Add(new SponsorData(name, avatar, website!, GetBubbleSize(value)));
	}
}
else
{
	var responseContent = File.ReadAllText(filename);
	var responseData = JsonNode.Parse(responseContent);
	sponsorData = new List<SponsorData>();
	var seenSponsors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	foreach (var x in responseData!["data"]!["user"]!["sponsorshipsAsMaintainer"]!["nodes"]!.AsArray())
	{
		var sponsorEntity = x!["sponsorEntity"];
		if (sponsorEntity is null) continue;

		var name = sponsorEntity["login"]?.GetValue<string>();
		var avatarString = sponsorEntity["avatarUrl"]?.GetValue<string>();
		if (name is null || avatarString is null) continue;
		if (!seenSponsors.Add(name)) continue;

		var avatar = new Uri(avatarString);
		var site = sponsorEntity["websiteUrl"]?.GetValue<string>();
		var website = site == null ? null : new Uri(site);
		var value = x["tier"]!["monthlyPriceInDollars"]!.GetValue<int>();

		sponsorData.Add(new SponsorData(name!, avatar!, website!, GetBubbleSize(value)));
	}
}

var options = new JsonSerializerOptions
{
	IncludeFields = true,
	PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	WriteIndented = true
};
var sponsorsWithBubbles = sponsorData.Where(x => x.BubbleSize != BubbleSize.None).ToList();
var featuredSponsorsJson = JsonSerializer.Serialize(sponsorsWithBubbles, options);
Console.WriteLine(featuredSponsorsJson);

File.WriteAllText("sponsor-data.json", featuredSponsorsJson);

return;

static BubbleSize GetBubbleSize(int value)
{
	if (value < 0) return BubbleSize.None;
	if (value < 10) return BubbleSize.Small;
	if (value < 25) return BubbleSize.Medium;
	return BubbleSize.Large;
}

[JsonConverter(typeof(JsonStringEnumConverter<BubbleSize>))]
public enum BubbleSize
{
	None,
	Small = 25,
	Medium = 35,
	Large = 50
}

public record SponsorData(string Username, Uri AvatarUrl, Uri WebsiteUrl, BubbleSize BubbleSize);
