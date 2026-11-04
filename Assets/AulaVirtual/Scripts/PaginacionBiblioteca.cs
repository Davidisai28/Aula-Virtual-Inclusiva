using System;

// Política de presentación independiente de Unity y de la conectividad.
public static class PaginacionBiblioteca
{
    public const int PorPagina = 2;
    public static int TotalPaginas(int total) => Math.Max(1, (Math.Max(0, total) + PorPagina - 1) / PorPagina);
    public static int Ajustar(int pagina, int total) => Math.Max(0, Math.Min(pagina, TotalPaginas(total) - 1));
    public static int Inicio(int pagina, int total) => Ajustar(pagina, total) * PorPagina;
    public static int Fin(int pagina, int total) => Math.Min(Math.Max(0, total), Inicio(pagina, total) + PorPagina);
}
