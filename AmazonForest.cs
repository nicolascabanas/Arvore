// AmazonForest.cs - v2: Flyweight introduzido — espécie como objeto compartilhado
using System;
using System.Collections.Generic;
using System.Linq;

// ── FLYWEIGHT: estado intrínseco (imutável, compartilhado) ──────────────────
class EspecieArvore
{
    public string Nome { get; }
    public string Cor { get; }
    public string Textura { get; }
    private readonly byte[] imagemCasca;    // ~500KB — compartilhada entre todas da espécie
    private readonly byte[] imagemFolhas;   // ~300KB — idem

    public EspecieArvore(string nome, string cor, string textura)
    {
        Nome = nome;
        Cor = cor;
        Textura = textura;
        imagemCasca = new byte[500_000];
        imagemFolhas = new byte[300_000];
        Console.WriteLine($"  [Flyweight criado] Espécie: {nome,-14} (+800KB na memória)");
    }

    public long GetMemoriaIntrinseca() =>
        imagemCasca.Length + imagemFolhas.Length
        + Nome.Length + Cor.Length + Textura.Length;

    public void Desenhar(double lat, double lon, double alt, int idade)
    {
        // Em produção: usa imagemCasca/Folhas para renderizar na posição
    }
}

// ── FLYWEIGHT FACTORY ────────────────────────────────────────────────────────
static class EspecieFactory
{
    private static readonly Dictionary<string, EspecieArvore> _pool = new();

    public static EspecieArvore GetEspecie(string nome, string cor, string textura)
    {
        if (!_pool.ContainsKey(nome))
            _pool[nome] = new EspecieArvore(nome, cor, textura);

        return _pool[nome]; // retorna instância compartilhada
    }

    public static int TotalEspeciesNaMemoria() => _pool.Count;
    public static long MemoriaDoPool() => _pool.Values.Sum(e => e.GetMemoriaIntrinseca());
}

// ── CONTEXTO: estado extrínseco (único por árvore, leve) ───────────────────
class ArvoreContexto
{
    private readonly EspecieArvore _especie; // referência 
    private readonly double _latitude;
    private readonly double _longitude;
    private readonly double _altura;
    private readonly int _idade;

    public ArvoreContexto(EspecieArvore especie, double lat, double lon, double alt, int idade)
    {
        _especie = especie;
        _latitude = lat;
        _longitude = lon;
        _altura = alt;
        _idade = idade;
    }

    // Memória real deste objeto: só os dados extrínsecos + 1 referência (8 bytes)
    public long GetMemoriaBytes() =>
        sizeof(double) * 3 + sizeof(int) + IntPtr.Size;

    public void Desenhar() => _especie.Desenhar(_latitude, _longitude, _altura, _idade);

    public override string ToString() =>
        $"[{_especie.Nome}] lat={_latitude:F4} lon={_longitude:F4} alt={_altura:F1}m";
}

// ── FLORESTA COM FLYWEIGHT ────────────────────────────────────────────────────
class FlorestaComFlyweight
{
    private readonly List<ArvoreContexto> _arvores = new();

    public void PlantarArvore(string especie, string cor, string textura,
                               double lat, double lon, double alt, int idade)
    {
        var esp = EspecieFactory.GetEspecie(especie, cor, textura);
        _arvores.Add(new ArvoreContexto(esp, lat, lon, alt, idade));
    }

    public long CalcularMemoriaContextos() => _arvores.Sum(a => a.GetMemoriaBytes());
    public long CalcularMemoriaTotal() => CalcularMemoriaContextos() + EspecieFactory.MemoriaDoPool();
    public int TotalArvores() => _arvores.Count;
}

class AmazonForest
{
    static readonly string[] Especies = { "Castanheira", "Seringueira", "Açaizeiro", "Mogno", "Andiroba" };
    static readonly Random rng = new();

    static void Main()
    {
        int totalArvores = 100_000;

        Console.WriteLine("=== COMMIT 2: Com Flyweight ===\n");
        Console.WriteLine("Espécies sendo registradas no pool:");

        var floresta = new FlorestaComFlyweight();

        for (int i = 0; i < totalArvores; i++)
        {
            string esp = Especies[i % Especies.Length];
            floresta.PlantarArvore(
                esp, $"verde-{esp}", $"rugosa-{esp}",
                -3.0 + rng.NextDouble() * 6,
                -73.0 + rng.NextDouble() * 20,
                5 + rng.NextDouble() * 40,
                rng.Next(1, 200)
            );
        }

        Console.WriteLine($"\nÁrvores plantadas        : {floresta.TotalArvores():N0}");
        Console.WriteLine($"Espécies no pool         : {EspecieFactory.TotalEspeciesNaMemoria()}");
        Console.WriteLine($"Memória do pool          : {EspecieFactory.MemoriaDoPool():N0} bytes");
        Console.WriteLine($"Memória dos contextos    : {floresta.CalcularMemoriaContextos():N0} bytes");
        Console.WriteLine($"Memória total            : {floresta.CalcularMemoriaTotal():N0} bytes");
        Console.WriteLine($"                         = {floresta.CalcularMemoriaTotal() / 1_000_000.0:F2} MB");
        Console.WriteLine($"\n✅ Apenas {EspecieFactory.TotalEspeciesNaMemoria()} objetos pesados na memória, não {totalArvores:N0}!");
    }
}