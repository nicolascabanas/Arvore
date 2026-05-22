// AmazonForest.cs - v3: Comparação lado a lado dos dois cenários
using System;
using System.Collections.Generic;
using System.Linq;


//  FLYWEIGHT — estado intrínseco

class EspecieArvore
{
    public string Nome    { get; }
    public string Cor     { get; }
    public string Textura { get; }
    private readonly byte[] imagemCasca;
    private readonly byte[] imagemFolhas;

    public EspecieArvore(string nome, string cor, string textura)
    {
        Nome         = nome;
        Cor          = cor;
        Textura      = textura;
        imagemCasca  = new byte[500_000];
        imagemFolhas = new byte[300_000];
    }

    public long GetMemoriaIntrinseca() =>
        imagemCasca.Length + imagemFolhas.Length + Nome.Length + Cor.Length + Textura.Length;
}

static class EspecieFactory
{
    private static readonly Dictionary<string, EspecieArvore> _pool = new();

    public static EspecieArvore GetEspecie(string nome, string cor, string textura)
    {
        if (!_pool.ContainsKey(nome))
            _pool[nome] = new EspecieArvore(nome, cor, textura);
        return _pool[nome];
    }

    public static void Reset() => _pool.Clear();
    public static int Count   => _pool.Count;
    public static long MemoriaPool => _pool.Values.Sum(e => e.GetMemoriaIntrinseca());
}


//  CENÁRIO A — sem Flyweight

class ArvoreSemFlyweight
{
    private readonly string especie, cor, textura;
    private readonly byte[] imagemCasca  = new byte[500_000];
    private readonly byte[] imagemFolhas = new byte[300_000];
    private readonly double lat, lon, alt;
    private readonly int idade;

    public ArvoreSemFlyweight(string especie, string cor, string textura,
                               double lat, double lon, double alt, int idade)
    {
        this.especie = especie; this.cor = cor; this.textura = textura;
        this.lat = lat; this.lon = lon; this.alt = alt; this.idade = idade;
    }

    public long GetMemoriaBytes() =>
        imagemCasca.Length + imagemFolhas.Length
        + especie.Length + cor.Length + textura.Length
        + sizeof(double) * 3 + sizeof(int);
}


//  CENÁRIO B — com Flyweight
class ArvoreContexto
{
    private readonly EspecieArvore _especie;
    private readonly double lat, lon, alt;
    private readonly int idade;

    public ArvoreContexto(EspecieArvore especie, double lat, double lon, double alt, int idade)
    {
        _especie = especie;
        this.lat = lat; this.lon = lon; this.alt = alt; this.idade = idade;
    }

    public long GetMemoriaBytes() => sizeof(double) * 3 + sizeof(int) + IntPtr.Size;
}


class AmazonForest
{
    static readonly string[] Especies = { "Castanheira", "Seringueira", "Açaizeiro", "Mogno", "Andiroba" };
    static readonly Random rng = new();

    static void Main()
    {
        int totalArvores = 100_000;

        Console.WriteLine("╔══════════════════════════════════════════════════╗");
        Console.WriteLine("║   Floresta Amazônica — Análise de Memória        ║");
        Console.WriteLine("╚══════════════════════════════════════════════════╝\n");

        // Cenário A: sem Flyweight 
        var semFly = new List<ArvoreSemFlyweight>();
        for (int i = 0; i < totalArvores; i++)
        {
            string esp = Especies[i % Especies.Length];
            semFly.Add(new ArvoreSemFlyweight(
                esp, $"verde-{esp}", $"rugosa-{esp}",
                -3.0 + rng.NextDouble() * 6,
                -73.0 + rng.NextDouble() * 20,
                5 + rng.NextDouble() * 40,
                rng.Next(1, 200)));
        }
        long memSem = semFly.Sum(a => a.GetMemoriaBytes());

        //  Cenário B: com Flyweight 
        EspecieFactory.Reset();
        var comFly = new List<ArvoreContexto>();
        for (int i = 0; i < totalArvores; i++)
        {
            string esp = Especies[i % Especies.Length];
            var especie = EspecieFactory.GetEspecie(esp, $"verde-{esp}", $"rugosa-{esp}");
            comFly.Add(new ArvoreContexto(
                especie,
                -3.0 + rng.NextDouble() * 6,
                -73.0 + rng.NextDouble() * 20,
                5 + rng.NextDouble() * 40,
                rng.Next(1, 200)));
        }
        long memContextos = comFly.Sum(a => a.GetMemoriaBytes());
        long memPool      = EspecieFactory.MemoriaPool;
        long memCom       = memContextos + memPool;

        // Relatório 
        Console.WriteLine($"Total de árvores : {totalArvores:N0}");
        Console.WriteLine($"Total de espécies: {EspecieFactory.Count}\n");

        Console.WriteLine("┌─────────────────────────────────────────────────┐");
        Console.WriteLine("│  CENÁRIO A — Sem Flyweight                      │");
        Console.WriteLine("├─────────────────────────────────────────────────┤");
        Console.WriteLine($"│  Memória total : {memSem,12:N0} bytes              │");
        Console.WriteLine($"│               = {memSem / 1_000_000.0,10:F2} MB                 │");
        Console.WriteLine($"│  Por árvore   : {(double)memSem / totalArvores,10:F0} bytes               │");
        Console.WriteLine("└─────────────────────────────────────────────────┘\n");

        Console.WriteLine("┌─────────────────────────────────────────────────┐");
        Console.WriteLine("│  CENÁRIO B — Com Flyweight                      │");
        Console.WriteLine("├─────────────────────────────────────────────────┤");
        Console.WriteLine($"│  Pool de espécies : {memPool,10:N0} bytes           │");
        Console.WriteLine($"│  Contextos        : {memContextos,10:N0} bytes           │");
        Console.WriteLine($"│  Total            : {memCom,10:N0} bytes           │");
        Console.WriteLine($"│                 = {memCom / 1_000_000.0,10:F2} MB                │");
        Console.WriteLine($"│  Por árvore     : {(double)memCom / totalArvores,10:F0} bytes             │");
        Console.WriteLine("└─────────────────────────────────────────────────┘\n");

        double reducao = (1.0 - (double)memCom / memSem) * 100;
        Console.WriteLine($"🌿 Redução de memória com Flyweight: {reducao:F1}%");
        Console.WriteLine($"🌿 Economia absoluta               : {(memSem - memCom) / 1_000_000.0:F2} MB");
    }
}