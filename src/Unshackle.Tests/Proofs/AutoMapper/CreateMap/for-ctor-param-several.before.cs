using System;
using AutoMapper;

namespace Shop;

public class CreateSale
{
    public string? Nome { get; set; }
    public decimal Desconto { get; set; }
    public DateTime DataInicio { get; set; }
}

public class SaleModel
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string? Nome { get; private set; }
    public decimal Desconto { get; private set; }
    public DateTime DataInicio { get; private set; }

    public SaleModel()
    {
    }

    public SaleModel(string nome, decimal desconto, DateTime dataInicio)
    {
        Nome = nome;
        Desconto = desconto;
        DataInicio = dataInicio;
    }
}

public class SaleProfile : Profile
{
    public SaleProfile()
    {
        CreateMap<CreateSale, SaleModel>()
            .ForCtorParam("nome", opt => opt.MapFrom(src => src.Nome))
            .ForCtorParam("desconto", opt => opt.MapFrom(src => src.Desconto))
            .ForCtorParam("dataInicio", opt => opt.MapFrom(src => src.DataInicio));
    }
}
