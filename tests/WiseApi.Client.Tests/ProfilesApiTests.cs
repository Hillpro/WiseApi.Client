using WiseApi.Client.Models.Profiles;
using WiseApi.Client.Services;
using WiseApi.Client.Tests.Infrastructure;

namespace WiseApi.Client.Tests;

public sealed class ProfilesApiTests
{
    [Fact]
    public async Task ListAsync_deserializes_mixed_personal_and_business_profiles()
    {
        const string body = """
        [
          {
            "type": "PERSONAL",
            "id": 14575282,
            "publicId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
            "userId": 9889627,
            "firstName": "Sarah",
            "lastName": "Jenkins",
            "fullName": "Sarah Jenkins",
            "email": "sarah.jenkins@example.com",
            "currentState": "VISIBLE",
            "createdAt": "2023-01-15T10:30:00",
            "updatedAt": "2025-06-18T14:20:00"
          },
          {
            "type": "BUSINESS",
            "id": 14599371,
            "publicId": "f0e9d8c7-b6a5-4321-fedc-ba9876543210",
            "userId": 9889627,
            "businessName": "Innovate Solutions Ltd",
            "registrationNumber": "SC1234567890ABCD",
            "companyType": "LIMITED_COMPANY",
            "currentState": "VISIBLE",
            "fullName": "Innovate Solutions Ltd"
          }
        ]
        """;
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson(body);
        var api = new ProfilesApi(http);

        var profiles = await api.ListAsync(CancellationToken.None);

        Assert.Equal(2, profiles.Count);
        var personal = Assert.IsType<PersonalProfile>(profiles[0]);
        Assert.Equal(14575282L, personal.Id);
        Assert.Equal("Sarah", personal.FirstName);
        Assert.Equal(ProfileType.Personal, personal.Kind);
        Assert.Equal(ProfileState.Visible, personal.CurrentState);

        var business = Assert.IsType<BusinessProfile>(profiles[1]);
        Assert.Equal("Innovate Solutions Ltd", business.BusinessName);
        Assert.Equal(ProfileType.Business, business.Kind);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/v2/profiles", request.Uri.AbsolutePath);
    }

    [Fact]
    public async Task GetAsync_hits_correct_path()
    {
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson("""{"type":"PERSONAL","id":42,"userId":7,"firstName":"Ada","fullName":"Ada Lovelace"}""");
        var api = new ProfilesApi(http);

        var profile = await api.GetAsync(42, CancellationToken.None);

        var personal = Assert.IsType<PersonalProfile>(profile);
        Assert.Equal("Ada", personal.FirstName);
        Assert.Equal("/v2/profiles/42", Assert.Single(handler.Requests).Uri.AbsolutePath);
    }
}
