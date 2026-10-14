using System;
using AutoMapper;

namespace Federation;

public interface IUriFactory
{
    Uri Create(Guid id);
}

public class Actor
{
    public Guid Id { get; set; }
}

public class ActorIdConverter : ITypeConverter<Actor, Uri>
{
    private readonly IUriFactory _uris;

    public ActorIdConverter(IUriFactory uris)
    {
        _uris = uris;
    }

    public Uri Convert(Actor source, Uri destination, ResolutionContext context) => _uris.Create(source.Id);
}

public class ActorProfile : Profile
{
    public ActorProfile()
    {
        CreateMap<Actor, Uri>().ConvertUsing<ActorIdConverter>();
    }
}
