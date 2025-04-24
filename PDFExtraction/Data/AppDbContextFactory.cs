using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PDFExtraction.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = "Server=localhost;Database=pdfextractdatabase;Uid=root;Pwd=TTpNo394;ConnectionTimeout=30;Protocol=pipe;SslMode=Disabled;";

        optionsBuilder.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString)
        );
        return new AppDbContext(optionsBuilder.Options);
    }
}