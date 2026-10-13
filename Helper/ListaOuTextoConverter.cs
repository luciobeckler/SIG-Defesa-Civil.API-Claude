using System.Text.Json;
using System.Text.Json.Serialization;

namespace SIG_Defesa_Civil.API.Helper
{
    /// <summary>
    /// Lê uma lista de texto aceitando <b>também</b> um texto único no JSON.
    /// </summary>
    /// <remarks>
    /// Existe por causa da fila offline. Um tablet que ficou dias em campo guarda
    /// o formulário da vistoria no formato da versão do app que o gravou. Quando
    /// <c>CaracterizacaoDoLocal</c> e <c>RegimeOcupacao</c> deixaram de ser texto
    /// e viraram lista, esse payload antigo passou a falhar na desserialização
    /// com HTTP 400 — e a vistoria ficava presa no aparelho, marcada como ERROR,
    /// até alguém reinstalar o app ou desistir dela.
    ///
    /// Com este conversor, <c>"PROPRIO"</c> entra como <c>["PROPRIO"]</c> e a fila
    /// antiga sobe sozinha na primeira sincronização. Texto vazio vira lista vazia.
    ///
    /// Pode ser removido quando não houver mais aparelho com fila anterior a
    /// outubro de 2026 — o que só se sabe conferindo os tablets, então o barato
    /// é deixar ficar.
    /// </remarks>
    public class ListaOuTextoConverter : JsonConverter<List<string>>
    {
        public override List<string> Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return new List<string>();

                // O formato antigo: um valor só.
                case JsonTokenType.String:
                {
                    var valor = reader.GetString();
                    return string.IsNullOrWhiteSpace(valor)
                        ? new List<string>()
                        : new List<string> { valor };
                }

                case JsonTokenType.StartArray:
                {
                    var itens = new List<string>();
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            var item = reader.GetString();
                            if (!string.IsNullOrWhiteSpace(item))
                                itens.Add(item);
                        }
                        else
                        {
                            reader.Skip();
                        }
                    }
                    return itens;
                }

                default:
                    throw new JsonException(
                        $"Esperava texto ou lista de textos, veio {reader.TokenType}.");
            }
        }

        /// <summary>Na saída é sempre lista — quem lê a API nova já espera isso.</summary>
        public override void Write(
            Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value ?? new List<string>())
                writer.WriteStringValue(item);
            writer.WriteEndArray();
        }
    }
}
