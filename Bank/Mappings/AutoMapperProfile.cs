using AutoMapper;
using Bank.DTO;
using Bank.Models;
namespace Bank.Mappings;
public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Account, AccountDto>().ReverseMap();
        CreateMap<Transaction, TransactionDto>().ReverseMap();
    }
}