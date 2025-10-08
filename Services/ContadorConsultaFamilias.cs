namespace WebApiAlmacen.Services
{
    public class ContadorConsultaFamilias
    {
        public int Total { get; set; } = 0;
        public void Add()
        {
            Total++;
        }
        public int GetTotal()
        {
            return Total;
        }
    }
}
