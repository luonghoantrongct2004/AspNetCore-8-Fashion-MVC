namespace App_Web.Models.ViewModel
{
    public class ProductViewModel
    {
        public Product Product { get; set; }
        public List<Product> RelatedProducts { get; set; }
        public List<Comment> Comments { get; set; }
        public bool IsReceived;
        public bool UserHasRated;
    }
}
