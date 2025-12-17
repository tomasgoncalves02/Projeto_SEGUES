namespace Projeto_SEGUES.Models
{
    public class DbStats
    {
        public int Id { get; set; }
        public string Table { get; set; }
        public int RowsNumb { get; set; }
        public decimal SpaceKb { get; set; }
        public decimal SpaceReserved { get; set; }
        public DateTime Date { get; set; }
    }
}
