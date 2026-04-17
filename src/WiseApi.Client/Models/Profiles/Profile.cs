using System.Text.Json.Serialization;

namespace WiseApi.Client.Models.Profiles;

/// <summary>Kind of Wise profile.</summary>
public enum ProfileType
{
    /// <summary>An individual.</summary>
    Personal,

    /// <summary>A legal entity.</summary>
    Business,
}

/// <summary>Visibility state of a Wise profile.</summary>
public enum ProfileState
{
    /// <summary>Hidden from the user's dashboard.</summary>
    Hidden,

    /// <summary>Visible.</summary>
    Visible,

    /// <summary>Deactivated.</summary>
    Deactivated,
}

/// <summary>An address associated with a profile.</summary>
public sealed record Address(
    long Id,
    string? AddressFirstLine,
    string? City,
    string? CountryIso2Code,
    string? CountryIso3Code,
    string? PostCode,
    string? StateCode);

/// <summary>Contact details associated with a profile.</summary>
public sealed record ContactDetails(string? Email, string? PhoneNumber);

/// <summary>
/// A Wise profile. Use <see cref="Type"/> to discriminate between <see cref="PersonalProfile"/> and
/// <see cref="BusinessProfile"/>, or pattern-match directly.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(PersonalProfile), typeDiscriminator: "PERSONAL")]
[JsonDerivedType(typeof(BusinessProfile), typeDiscriminator: "BUSINESS")]
public abstract class Profile
{
    /// <summary>Unique identifier for the profile.</summary>
    public long Id { get; init; }

    /// <summary>Publicly accessible identifier.</summary>
    public string? PublicId { get; init; }

    /// <summary>The user ID that owns this profile.</summary>
    public long UserId { get; init; }

    /// <summary>Primary address of the profile.</summary>
    public Address? Address { get; init; }

    /// <summary>Primary email on file.</summary>
    public string? Email { get; init; }

    /// <summary>Time the profile was created.</summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>Time the profile was last updated.</summary>
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>Current visibility state.</summary>
    public ProfileState? CurrentState { get; init; }

    /// <summary>Contact details.</summary>
    public ContactDetails? ContactDetails { get; init; }

    /// <summary>Full display name.</summary>
    public string? FullName { get; init; }

    /// <summary>
    /// Discriminator. Equivalent to testing <c>profile is PersonalProfile</c> / <c>is BusinessProfile</c>,
    /// but handy when storing or switching on the value.
    /// </summary>
    [JsonIgnore]
    public abstract ProfileType Kind { get; }
}

/// <summary>A personal Wise profile.</summary>
public sealed class PersonalProfile : Profile
{
    /// <inheritdoc />
    public override ProfileType Kind => ProfileType.Personal;

    /// <summary>Avatar URL.</summary>
    public string? Avatar { get; init; }

    /// <summary>Given name.</summary>
    public string? FirstName { get; init; }

    /// <summary>Family name.</summary>
    public string? LastName { get; init; }

    /// <summary>Preferred name.</summary>
    public string? PreferredName { get; init; }

    /// <summary>Date of birth (ISO 8601 date, unparsed).</summary>
    public string? DateOfBirth { get; init; }

    /// <summary>Phone number.</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>Additional addresses.</summary>
    public IReadOnlyList<Address>? SecondaryAddresses { get; init; }
}

/// <summary>A business Wise profile.</summary>
public sealed class BusinessProfile : Profile
{
    /// <inheritdoc />
    public override ProfileType Kind => ProfileType.Business;

    /// <summary>Registered business name.</summary>
    public string? BusinessName { get; init; }

    /// <summary>Business registration number.</summary>
    public string? RegistrationNumber { get; init; }

    /// <summary>Short description of the business.</summary>
    public string? DescriptionOfBusiness { get; init; }

    /// <summary>Public webpage.</summary>
    public string? Webpage { get; init; }

    /// <summary>Company type.</summary>
    public string? CompanyType { get; init; }

    /// <summary>Role of the profile manager.</summary>
    public string? CompanyRole { get; init; }

    /// <summary>Free-form business description.</summary>
    public string? BusinessFreeFormDescription { get; init; }

    /// <summary>Primary business category.</summary>
    public string? FirstLevelCategory { get; init; }

    /// <summary>Secondary business category.</summary>
    public string? SecondLevelCategory { get; init; }

    /// <summary>Operational addresses.</summary>
    public IReadOnlyList<Address>? OperationalAddresses { get; init; }
}
