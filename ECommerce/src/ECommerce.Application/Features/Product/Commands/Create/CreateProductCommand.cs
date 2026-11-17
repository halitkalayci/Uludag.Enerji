using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Features.Product.Commands.Create
{
    public class CreateProductCommand // komut varolmadan
    {
        // İki parçadan oluşur -> Komutun gövdesi, (Create işlemi yapılırken ne istiyosun?)

        public string Name { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public int Stock { get; set; }

        // İkinci parça -> Commandın handleri 
        // Bu komut geldiğinde ne olacak?
        // Gövdeyi aldı işlemi yapacak.


        // varolması mantıklı..
        public class CreateProductCommandHandler // Komut handlerı, işleyen kişi.
        {
            public void Handle(CreateProductCommand command)
            {
                Console.WriteLine($"Ürün oluşturuldu..");
            }
        }
    }
}
