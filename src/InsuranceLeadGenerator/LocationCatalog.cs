namespace InsuranceData;

internal sealed class LocationCatalog
{
    private readonly LocationEntry[] _locations =
    {
        new("AL", "Birmingham", "Jefferson", "35203", new[] { "205", "659" }, 2),
        new("AK", "Anchorage", "Anchorage", "99501", new[] { "907" }, 1),
        new("AZ", "Phoenix", "Maricopa", "85004", new[] { "602", "480", "623" }, 7),
        new("AR", "Little Rock", "Pulaski", "72201", new[] { "501" }, 1),
        new("CA", "Sacramento", "Sacramento", "95814", new[] { "279", "916" }, 9),
        new("CA", "Fresno", "Fresno", "93721", new[] { "559" }, 9),
        new("CO", "Denver", "Denver", "80202", new[] { "303", "720" }, 3),
        new("CT", "Hartford", "Hartford", "06103", new[] { "860", "959" }, 2),
        new("DE", "Wilmington", "New Castle", "19801", new[] { "302" }, 1),
        new("FL", "Orlando", "Orange", "32801", new[] { "321", "407", "689" }, 8),
        new("FL", "Tampa", "Hillsborough", "33602", new[] { "656", "813" }, 8),
        new("GA", "Atlanta", "Fulton", "30303", new[] { "404", "470", "678" }, 6),
        new("HI", "Honolulu", "Honolulu", "96813", new[] { "808" }, 1),
        new("ID", "Boise", "Ada", "83702", new[] { "208", "986" }, 1),
        new("IL", "Peoria", "Peoria", "61602", new[] { "309" }, 6),
        new("IL", "Chicago", "Cook", "60601", new[] { "312", "773", "872" }, 6),
        new("IN", "Indianapolis", "Marion", "46204", new[] { "317", "463" }, 2),
        new("IA", "Des Moines", "Polk", "50309", new[] { "515" }, 1),
        new("KS", "Wichita", "Sedgwick", "67202", new[] { "316" }, 1),
        new("KY", "Louisville", "Jefferson", "40202", new[] { "502" }, 2),
        new("LA", "Baton Rouge", "East Baton Rouge", "70801", new[] { "225" }, 2),
        new("ME", "Portland", "Cumberland", "04101", new[] { "207" }, 1),
        new("MD", "Baltimore", "Baltimore City", "21202", new[] { "410", "443", "667" }, 3),
        new("MA", "Worcester", "Worcester", "01608", new[] { "508", "774" }, 3),
        new("MI", "Grand Rapids", "Kent", "49503", new[] { "616" }, 5),
        new("MI", "Lansing", "Ingham", "48933", new[] { "517" }, 5),
        new("MN", "Minneapolis", "Hennepin", "55401", new[] { "612", "763", "952" }, 3),
        new("MS", "Jackson", "Hinds", "39201", new[] { "601", "769" }, 1),
        new("MO", "Columbia", "Boone", "65201", new[] { "573" }, 2),
        new("MT", "Billings", "Yellowstone", "59101", new[] { "406" }, 1),
        new("NE", "Omaha", "Douglas", "68102", new[] { "402", "531" }, 1),
        new("NV", "Reno", "Washoe", "89501", new[] { "775" }, 2),
        new("NH", "Manchester", "Hillsborough", "03101", new[] { "603" }, 1),
        new("NJ", "Newark", "Essex", "07102", new[] { "862", "973" }, 3),
        new("NM", "Albuquerque", "Bernalillo", "87102", new[] { "505" }, 1),
        new("NY", "Buffalo", "Erie", "14202", new[] { "716" }, 7),
        new("NY", "Albany", "Albany", "12207", new[] { "518", "838" }, 7),
        new("NC", "Raleigh", "Wake", "27601", new[] { "919", "984" }, 5),
        new("ND", "Fargo", "Cass", "58102", new[] { "701" }, 1),
        new("OH", "Columbus", "Franklin", "43215", new[] { "380", "614" }, 6),
        new("OH", "Toledo", "Lucas", "43604", new[] { "419", "567" }, 6),
        new("OK", "Tulsa", "Tulsa", "74103", new[] { "539", "918" }, 2),
        new("OR", "Salem", "Marion", "97301", new[] { "503", "971" }, 5),
        new("PA", "Allentown", "Lehigh", "18101", new[] { "484", "610" }, 7),
        new("PA", "Harrisburg", "Dauphin", "17101", new[] { "717", "223" }, 7),
        new("RI", "Providence", "Providence", "02903", new[] { "401" }, 1),
        new("SC", "Columbia", "Richland", "29201", new[] { "803" }, 2),
        new("SD", "Sioux Falls", "Minnehaha", "57104", new[] { "605" }, 1),
        new("TN", "Knoxville", "Knox", "37902", new[] { "865" }, 3),
        new("TX", "Fort Worth", "Tarrant", "76102", new[] { "682", "817" }, 9),
        new("TX", "Austin", "Travis", "78701", new[] { "512", "737" }, 9),
        new("UT", "Salt Lake City", "Salt Lake", "84111", new[] { "385", "801" }, 2),
        new("VT", "Burlington", "Chittenden", "05401", new[] { "802" }, 1),
        new("VA", "Richmond", "Richmond City", "23219", new[] { "804" }, 3),
        new("WA", "Tacoma", "Pierce", "98402", new[] { "253" }, 5),
        new("WA", "Spokane", "Spokane", "99201", new[] { "509" }, 5),
        new("WV", "Charleston", "Kanawha", "25301", new[] { "304", "681" }, 1),
        new("WI", "Madison", "Dane", "53703", new[] { "608" }, 2),
        new("WY", "Cheyenne", "Laramie", "82001", new[] { "307" }, 1),
    };

    private readonly int[] _cumulativeWeights;
    private readonly int _totalWeight;

    public LocationCatalog()
    {
        _cumulativeWeights = new int[_locations.Length];
        int running = 0;
        for (int i = 0; i < _locations.Length; i++)
        {
            running += _locations[i].Weight;
            _cumulativeWeights[i] = running;
        }

        _totalWeight = running;
    }

    public LocationEntry PickLocation(DeterministicRandom random)
    {
        int sample = random.NextInt(_totalWeight);
        for (int i = 0; i < _cumulativeWeights.Length; i++)
        {
            if (sample < _cumulativeWeights[i])
            {
                return _locations[i];
            }
        }

        return _locations[^1];
    }
}

internal sealed record LocationEntry(string StateCode, string City, string County, string ZipCode, string[] AreaCodes, int Weight);
