using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Data;

namespace Application.Reportes
{
    public static class ReportePdfService
    {
        static ReportePdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static byte[] GenerarPdfPersonasMovilizador(
            DataTable table,
            string titulo = "Reporte de Personas del Movilizador",
            string? subtitulo = null)
        {
            return GenerarPdfTabla(
                titulo: titulo,
                subtitulo: subtitulo,
                table: table,
                columnas: new[]
                {
                    ("Nombres", "Nombres"),
                    ("Apellidos", "Apellidos"),
                    ("CI", "CI"),
                    ("Celular", "Celular"),
                    ("Estado", "EstadoDiaD"),
                }
            );
        }

        public static byte[] GenerarPdfUsuarios(DataTable table)
        {
            return GenerarPdfTabla(
                titulo: "Reporte de Usuarios",
                subtitulo: "Listado general de usuarios del sistema",
                table: table,
                columnas: new[]
                {
                    ("Usuario", "Usuario"),
                    ("Nombre", "NombreCompleto"),
                    ("Rol", "Rol"),
                    ("Territorio", "Territorio"),
                    ("Celular", "Celular"),
                    ("Activo", "Activo"),
                }
            );
        }

        public static byte[] GenerarPdfTerritorios(DataTable table)
        {
            return GenerarPdfTabla(
                titulo: "Reporte de Territorios",
                subtitulo: "Estructura territorial registrada",
                table: table,
                columnas: new[]
                {
                    ("Nombre", "Nombre"),
                    ("Tipo", "TipoTerritorio"),
                    ("Padre", "NombrePadre"),
                    ("Código", "Codigo"),
                    ("Activo", "Activo"),
                }
            );
        }

        public static byte[] GenerarPdfMovilizadoresGerente(DataTable table)
        {
            return GenerarPdfTabla(
                titulo: "Reporte de Movilizadores",
                subtitulo: "Movilizadores del territorio del gerente",
                table: table,
                columnas: new[]
                {
                    ("Movilizador", "Movilizador"),
                    ("Meta", "MetaObjetivo"),
                    ("Registrados", "TotalRegistrados"),
                    ("Ya votó", "TotalYaVoto"),
                    ("No contactado", "TotalNoContactado"),
                    ("% Cumpl.", "PorcentajeCumplimiento"),
                    ("Semáforo", "Semaforo"),
                }
            );
        }

        public static byte[] GenerarPdfAlertas(DataTable table, string titulo = "Reporte de Alertas")
        {
            return GenerarPdfTabla(
                titulo: titulo,
                subtitulo: "Alertas operativas registradas",
                table: table,
                columnas: new[]
                {
                    ("Movilizador", "Movilizador"),
                    ("Tipo", "TipoAlerta"),
                    ("Nivel", "Nivel"),
                    ("Estado", "Estado"),
                    ("Descripción", "Descripcion"),
                    ("Fecha", "FechaGeneracion"),
                }
            );
        }

        public static byte[] GenerarPdfTabla(
            string titulo,
            string? subtitulo,
            DataTable table,
            (string Header, string Field)[] columnas)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4.Landscape());
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(titulo)
                            .SemiBold().FontSize(18).FontColor(Colors.Blue.Darken2);

                        if (!string.IsNullOrWhiteSpace(subtitulo))
                            col.Item().Text(subtitulo)
                                .FontSize(10).FontColor(Colors.Grey.Darken1);

                        col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingVertical(10).Table(tbl =>
                    {
                        tbl.ColumnsDefinition(columns =>
                        {
                            foreach (var _ in columnas)
                                columns.RelativeColumn();
                        });

                        tbl.Header(header =>
                        {
                            foreach (var columna in columnas)
                            {
                                header.Cell().Element(CellHeaderStyle).Text(columna.Header);
                            }
                        });

                        foreach (DataRow row in table.Rows)
                        {
                            foreach (var columna in columnas)
                            {
                                var valor = row.Table.Columns.Contains(columna.Field)
                                    ? row[columna.Field]?.ToString() ?? ""
                                    : "";

                                tbl.Cell().Element(CellBodyStyle).Text(valor);
                            }
                        }
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text($"Total registros: {table.Rows.Count}")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);

                        row.ConstantItem(180).AlignRight().Text(
                            $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}"
                        ).FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                });
            }).GeneratePdf();
        }

        static IContainer CellHeaderStyle(IContainer container)
        {
            return container
                .Background(Colors.Blue.Lighten4)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(5);
        }

        static IContainer CellBodyStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten3)
                .Padding(4);
        }
    }
}