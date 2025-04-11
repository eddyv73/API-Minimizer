using System;
using System.Collections.Generic;

namespace BancoApp
{
    public class Tarjeta
    {
        // Identificación y datos bancarios
        public string NumeroTarjeta { get; set; }
        public string NumeroTarjetaEnmascarado { get; set; }
        public string TipoTarjeta { get; set; }  // Débito, Crédito, Prepago
        public string EntidadEmisora { get; set; }
        public string RedProcesadora { get; set; }  // Visa, Mastercard, American Express
        public string BIN { get; set; }  // Bank Identification Number (6 primeros dígitos)
        
        // Datos del titular
        public string NombreTitular { get; set; }
        public string ApellidoTitular { get; set; }
        public string Identificacion { get; set; }
        public string DireccionFacturacion { get; set; }
        public string CorreoElectronico { get; set; }
        public string NumeroTelefono { get; set; }
        
        // Fechas y validez
        public DateTime FechaEmision { get; set; }
        public DateTime FechaExpiracion { get; set; }
        public bool EstaActiva { get; set; }
        public bool EstaBloqueada { get; set; }
        public string MotivoBloqueo { get; set; }
        
        // Seguridad
        public int CodigoSeguridad { get; set; }  // CVV/CVC
        public string PIN { get; set; }
        public bool TieneChip { get; set; }
        public bool TieneContactless { get; set; }
        public int IntentosFallidos { get; set; }
        
        // Financieros
        public decimal LimiteCredito { get; set; }
        public decimal SaldoActual { get; set; }
        public decimal DisponibleActual { get; set; }
        public decimal TasaInteresAnual { get; set; }
        public string MonedaPrincipal { get; set; }
        public int DiaPago { get; set; }
        public DateTime FechaUltimoPago { get; set; }
        
        // Programa de recompensas
        public bool ParticipaEnProgramaPuntos { get; set; }
        public int PuntosAcumulados { get; set; }
        public decimal ValorPunto { get; set; }
        
        // Constructor básico
        public Tarjeta()
        {
            FechaEmision = DateTime.Now;
            EstaActiva = false;
            EstaBloqueada = false;
            TieneChip = true;
            TieneContactless = true;
            IntentosFallidos = 0;
            MonedaPrincipal = "MXN";
        }
        
        // Método simple para enmascarar el número de tarjeta
        public void EnmascararNumero()
        {
            if (!string.IsNullOrEmpty(NumeroTarjeta) && NumeroTarjeta.Length >= 16)
            {
                NumeroTarjetaEnmascarado = $"**** **** **** {NumeroTarjeta.Substring(NumeroTarjeta.Length - 4)}";
            }
        }
        
        // Método para calcular el saldo disponible
        public void CalcularDisponible()
        {
            DisponibleActual = LimiteCredito - SaldoActual;
            if (DisponibleActual < 0)
            {
                DisponibleActual = 0;
            }
        }
    }
}