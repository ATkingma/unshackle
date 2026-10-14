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
        CreateMap<SigningKey, PublicKeyDto>()
            .ConvertUsing(src => new PublicKeyDto
            {
                PublicKeyPem = src.Pem,
                Created = src.Created,
            });
        CreateMap<Actor, ActorDto>();
    }
}

public class ActorService
{
    private readonly IMapper _mapper;

    public ActorService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public ActorDto Get(Actor actor) => _mapper.Map<ActorDto>(actor);

    public PublicKeyDto Key(SigningKey key) => _mapper.Map<PublicKeyDto>(key);
}
