using System.Collections.Generic;
using TLPB.Core.Dimensoes;

namespace TLPB.Runtime.Clientes
{
    /// <summary>
    /// Fila externa (GDD §4.3/§7.4). Clientes além da capacidade de mesas esperam aqui e
    /// entram SÓ no passo Início do Turno, nunca no meio da ação — evita loops de reação
    /// em cascata (§4.4). POCO.
    /// </summary>
    public class ClientQueue
    {
        private readonly Queue<ClienteData> _fila = new();

        public int Contagem => _fila.Count;
        public bool Vazia => _fila.Count == 0;

        public void Enfileirar(ClienteData cliente)
        {
            if (cliente != null) _fila.Enqueue(cliente);
        }

        public ClienteData Proximo() => _fila.Count > 0 ? _fila.Dequeue() : null;
    }
}
