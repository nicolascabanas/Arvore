using System;
using System.Collections.Generic;
using System.Linq;

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
        imagemCasca  = new byte[500000];
        imagemFolhas = new byte[300000];
    }

    public long GetMemoriaIntrinseca() =>
        imagemCasca.Length + imagemFolhas.Length + Nome.Length + Cor.Length + Textura.Length;
}

static class EspecieFactory
{
    private static readonly Dictionary<string, EspecieArvore> _pool = new Dictionary<string, EspecieArvore>();

    public static EspecieArvore GetEspecie(string nome, string cor, string textura)
    {
        if (!_pool.ContainsKey(nome))
            _pool[nome] = new EspecieArvore(nome, cor, textura);
        return _pool[nome];
    }

    public static void Reset()     => _pool.Clear();
    public static int Count        => _pool.Count;
    public static long MemoriaPool => _pool.Values.Sum(e => e.GetMemoriaIntrinseca());
}

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

static class Simulador
{
    static readonly string[] Especies = { "Castanheira", "Seringueira", "Acaizeiro", "Mogno", "Andiroba" };

    // Calcula memoria sem Flyweight de forma ANALITICA (sem alocar objetos reais)
    // Evita travar o processo com 1M x 800KB na heap
    static long CalcularMemoriaSemFlyweight(int totalArvores)
    {
        // cada arvore teria: 500000 + 300000 bytes de imagem + strings + doubles + int
        // strings: especie (~10) + cor (~15) + textura (~17) = ~42 bytes (media)
        long bytesPorArvore = 500000 + 300000 + 42 + sizeof(double) * 3 + sizeof(int);
        return bytesPorArvore * totalArvores;
    }

    public static (long semFly, long comFly, int numEspecies) Simular(int totalArvores)
    {
        long memSem = CalcularMemoriaSemFlyweight(totalArvores);

        EspecieFactory.Reset();
        var rng = new Random(42);
        long memContextos = 0;

        for (int i = 0; i < totalArvores; i++)
        {
            string esp = Especies[i % Especies.Length];
            var especie = EspecieFactory.GetEspecie(esp, "verde-" + esp, "rugosa-" + esp);
            var ctx = new ArvoreContexto(
                especie,
                -3.0  + rng.NextDouble() * 6,
                -73.0 + rng.NextDouble() * 20,
                5     + rng.NextDouble() * 40,
                rng.Next(1, 200));
            memContextos += ctx.GetMemoriaBytes();
        }

        long memCom = memContextos + EspecieFactory.MemoriaPool;
        return (memSem, memCom, EspecieFactory.Count);
    }
}

class AmazonForest
{
    static void Main()
    {
        int[] escalas = { 1000, 10000, 100000, 1000000 };

        Console.WriteLine("{0,-12} {1,-20} {2,-20} {3,-15} {4}",
            "Arvores", "Sem Flyweight", "Com Flyweight", "Economia", "Reducao");
        Console.WriteLine(new string('-', 75));

        foreach (int n in escalas)
        {
            var (semFly, comFly, _) = Simulador.Simular(n);
            double economia = (semFly - comFly) / 1000000.0;
            double reducao  = (1.0 - (double)comFly / semFly) * 100;

            Console.WriteLine("{0,-12} {1,-20} {2,-20} {3,-15} {4}",
                n,
                (semFly / 1000000.0).ToString("F2") + " MB",
                (comFly / 1000000.0).ToString("F2") + " MB",
                economia.ToString("F2") + " MB",
                reducao.ToString("F1") + "%");
        }

        Console.WriteLine();
        Console.WriteLine(new string('=', 65));
        Console.WriteLine("  DETALHAMENTO - 1.000.000 de arvores");
        Console.WriteLine(new string('=', 65));

        EspecieFactory.Reset();
        var (semD, comD, numEsp) = Simulador.Simular(1000000);

        Console.WriteLine("\n  Especies no pool Flyweight : " + numEsp);
        Console.WriteLine("  Memoria do pool (especies) : " + (EspecieFactory.MemoriaPool / 1000000.0).ToString("F2") + " MB");
        Console.WriteLine("  Memoria dos contextos      : " + ((comD - EspecieFactory.MemoriaPool) / 1000000.0).ToString("F2") + " MB");
        Console.WriteLine("\n  Sem Flyweight -> " + (semD / 1000000.0).ToString("F2") + " MB");
        Console.WriteLine("  Com Flyweight -> " + (comD  / 1000000.0).ToString("F2") + " MB");
        Console.WriteLine("\n  Economia      : " + ((semD - comD) / 1000000.0).ToString("F2") + " MB");
        Console.WriteLine("  Reducao       : " + ((1.0 - (double)comD / semD) * 100).ToString("F1") + "%");

        Console.WriteLine();
        Console.WriteLine(new string('=', 65));
        Console.WriteLine("  POR QUE FUNCIONA?");
        Console.WriteLine(new string('=', 65));
        Console.WriteLine("  Sem Flyweight : 800 KB x N arvores");
        Console.WriteLine("  Com Flyweight : 800 KB x 5 especies  +  36 B x N arvores");
        Console.WriteLine(new string('=', 65));
    }
}