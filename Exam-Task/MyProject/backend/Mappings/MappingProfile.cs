using AutoMapper;
using MyProject.Application.DTOs.Invoice;
using MyProject.Application.DTOs.QuickBooks;
using MyProject.Domain.Entities;

namespace MyProject.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Invoice, InvoiceDto>();
            CreateMap<InvoiceLineItem, InvoiceLineItemDto>();
            CreateMap<CreateInvoiceDto, Invoice>();
            CreateMap<CreateInvoiceLineItemDto, InvoiceLineItem>();
            CreateMap<Company, CompanyDto>();
        }
    }
}