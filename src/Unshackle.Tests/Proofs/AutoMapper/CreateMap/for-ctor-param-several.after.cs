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
    }
}

public static class SaleProfileExtensions
{
    public static SaleModel ToSaleModel(this CreateSale source) => new(source.Nome, source.Desconto, source.DataInicio);
}
