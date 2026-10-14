using System;
using AutoMapper;

namespace Keys;

public class SigningKey
{
    public string Pem { get; set; } = "";
    public DateTime Created { get; set; }
}

public class PublicKeyDto
{
    public string PublicKeyPem { get; set; } = "";
    public DateTime Created { get; set; }
}

public class Actor
{
    public string Name { get; set; } = "";
    public SigningKey Key { get; set; } = new();
}

public class ActorDto
{
    public string Name { get; set; } = "";
    public PublicKeyDto Key { get; set; } = new();
}

public class KeyProfile : Profile
{
    public KeyProfile()
    {
    }
}

public static class KeyProfileExtensions
{
    public static PublicKeyDto ToPublicKeyDto(this SigningKey source) => new PublicKeyDto
    {
        PublicKeyPem = source.Pem,
        Created = source.Created,
    };

    public static ActorDto ToActorDto(this Actor source) => new()
    {
        Name = source.Name,
        Key = source.Key.ToPublicKeyDto(),
    };
}

public class ActorService
{
    private readonly IMapper _mapper;

    public ActorService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public ActorDto Get(Actor actor) => actor.ToActorDto();

    public PublicKeyDto Key(SigningKey key) => key.ToPublicKeyDto();
}
