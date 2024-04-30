﻿using Facilit.Models;
using Facilit.Models.ClienteTiny;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Facilit.Servicos
{




    public class RetornoTinyApi
    {
        string tokenTiny = "02011b49e5399d62d999007a8952642c85cca50bc310b49fdd6c3674fdff4b2a";
        string formatoRetorno = "json";
        private readonly Conexao conexao;

        public RetornoTinyApi()
        {
            conexao = new Conexao();
        }

        public async Task<ProdutoTiny> ListarProdutos(string token)
        {
            var cliente = new HttpClient(); //intanciação de httpcliente

            var requisicao = new HttpRequestMessage(HttpMethod.Get, $"https://api.tiny.com.br/api2/pdv.produtos.php?token={tokenTiny}&formato={formatoRetorno}");// requisiçao sem o número de pagina

            var resposta = await cliente.SendAsync(requisicao); //enviando requisição

            if (resposta.IsSuccessStatusCode) //se a resposta for 200 entra no if
            {
                var respostaJson = await resposta.Content.ReadAsStringAsync(); // le a resposta conseguida por  var resposta e transforma em string

                var retornoTinyDeserializado = JsonSerializer.Deserialize<ProdutoTiny>(respostaJson); //convetendo obj json em string e armazenando no produtotiny

                if (retornoTinyDeserializado.retorno != null && retornoTinyDeserializado.retorno.numero_paginas > 0) //se hover  retorno e tiver paginas 
                {
                    var todosProdutos = new List<ProdutoTiny.Produto>(); //lista com as propiedades de produto

                    for (int i = 1; i < retornoTinyDeserializado.retorno.numero_paginas; i++)
                    {
                        var requisicaoPorPagina = new HttpRequestMessage(HttpMethod.Get, $"https://api.tiny.com.br/api2/pdv.produtos.php?token={tokenTiny}&formato={formatoRetorno}&pagina={i}");
                        var respostaPorPagina = await cliente.SendAsync(requisicaoPorPagina);
                        if (respostaPorPagina.IsSuccessStatusCode)
                        {
                            var respostaPorPaginaJson = await respostaPorPagina.Content.ReadAsStringAsync();

                            var retornoTinyDeserializadoPorPagina = JsonSerializer.Deserialize<ProdutoTiny>(respostaPorPaginaJson);
                            todosProdutos.AddRange(retornoTinyDeserializadoPorPagina.retorno.produtos.ToList());
                        }
                        continue;
                    }
                    if (todosProdutos.Any())
                    {
                        retornoTinyDeserializado.retorno.produtos = todosProdutos.ToArray();
                    }
                }

                await SalvarProdutosNoBanco(retornoTinyDeserializado.retorno.produtos);

                return retornoTinyDeserializado;
            }
            return default;
        }

        private async Task SalvarProdutosNoBanco(IEnumerable<ProdutoTiny.Produto> produtos)
        {

            string sql_insert_produtos = "insert into tb_produtos (codigo_tiny_produto, descricao, unidade, data_atualizacao_produto) values (@codigo_tiny_produto, @descricao, @unidade,@data_atualizacao_produto)";


            foreach (var produto in produtos)
            {
                using (var comando = new MySqlCommand(sql_insert_produtos, conexao._conn))
                {

                    comando.Parameters.AddWithValue("@codigo_tiny_produto", produto.id);

                    comando.Parameters.AddWithValue("@descricao", produto.descricao);

                    comando.Parameters.AddWithValue("@unidade", produto.unidade);

                    comando.Parameters.AddWithValue("@data_atualizacao_produto", DateTime.Now);

                    await comando.ExecuteNonQueryAsync();
                }
            }
        }
        public async Task<ClienteTiny> ListarClientes(string token)
        {
            var cliente = new HttpClient();

            var requisicao = new HttpRequestMessage(HttpMethod.Get, $"https://api.tiny.com.br/api2/contatos.pesquisa.php?token={token}&formato={formatoRetorno}");

            var resposta = await cliente.SendAsync(requisicao);

            if (resposta.IsSuccessStatusCode)
            {
                var respostaJson = await resposta.Content.ReadAsStringAsync();

                var retornoTinyDeserializado = JsonSerializer.Deserialize<ClienteTiny>(respostaJson);

                if (retornoTinyDeserializado.retorno != null && retornoTinyDeserializado.retorno.numero_paginas > 0)
                {
                    var todosClientes = new List<Contato1>();

                    for (int i = 1; i <= retornoTinyDeserializado.retorno.numero_paginas; i++)
                    {
                        var requisicaoPorPagina = new HttpRequestMessage(HttpMethod.Get, $"https://api.tiny.com.br/api2/contatos.pesquisa.php?token={token}&formato={formatoRetorno}&pagina={i}");

                        var respostaPorPagina = await cliente.SendAsync(requisicaoPorPagina);

                        if (respostaPorPagina.IsSuccessStatusCode)
                        {
                            var respostaPorPaginaJson = await respostaPorPagina.Content.ReadAsStringAsync();

                            try
                            {
                                var retornoTinyDeserializadoPorPagina = JsonSerializer.Deserialize<ClienteTiny>(respostaPorPaginaJson);
                                todosClientes.AddRange(retornoTinyDeserializadoPorPagina.retorno.contatos.Select(c => c.contato));
                            }
                            catch (Exception ex)
                            {
                                var erro = ex.Message;
                            }

                        }
                    }
                    if (todosClientes.Any())
                    {
                        retornoTinyDeserializado.retorno.contatos = todosClientes.Select(c => new Contato { contato = c }).ToArray();
                    }

                }
             

                return retornoTinyDeserializado;
            }

            return default;
        }

        private async Task SalvarClientesNoBanco(IEnumerable<Contato1> clientes)
        {
            string sql_insert_cliente = "insert into tb_clientes(codigo_tiny_cliente, nome, data_atualizacao_cliente) values" +
                "(@codigo_tiny_cliente, @nome, @data_atualizacao_cliente) ";
            foreach (var cliente in clientes)
            {
                using (var comando = new MySqlCommand(sql_insert_cliente, conexao._conn))
                {
                    comando.Parameters.AddWithValue("@codigo_tiny_cliente", cliente.id);
                    comando.Parameters.AddWithValue("@nome", cliente.nome);
                    comando.Parameters.AddWithValue(" @data_atualizacao_cliente", DateTime.Now);
                    await comando.ExecuteNonQueryAsync();
         
                }
            }
        }

    }



}