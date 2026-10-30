# 18 — Escenarios económicos CLP / USD

<!-- DOCUMENTACION:nivel-2026-10-29:BEGIN -->

**Nivel:** desarrollo del proyecto.

## Escenarios económicos de Squid Ink-Pulse

**Finalidad:** presupuestar decisiones futuras, no tasar el repositorio ni declarar costos/ventas históricos. Alcance modelado: **Cierre de un gate móvil con run física, UI y rendimiento**. No hay precio de venta o ingresos de equipo confirmados. Publicación gratuita no elimina tiempo de mantenimiento ni derechos/licencias de recursos.

### Moneda y supuestos

Conversión: **USD 1 = CLP 994,29**, valor del **27-10-2026** en la [tabla oficial SII](https://www.sii.cl/valores_y_fechas/dolar/dolar2026.htm), último día con cifra visible al consultar el 29-10-2026. No se atribuye ese valor al día 29 ni a una transacción bancaria. CLP = USD × R; USD = CLP / R, con R = 994,29.

Para valorizar esfuerzo se supone **CLP 15.000/h** (USD 15,09/h). Es una tasa elegida para comparar escenarios, no sueldo de mercado, tarifa profesional ni compromiso del responsable. Rangos de horas, mantenimiento y reservas también son supuestos; no se deducen de SCT, commits o líneas de código.

### Inversión del siguiente alcance

| Concepto                        | Base del escenario                       | CLP                     | USD                   |
| ------------------------------- | ---------------------------------------- | ----------------------- | --------------------- |
| Tiempo del siguiente alcance    | 24–56 h a CLP 15.000/h                   | CLP 360.000–CLP 840.000 | USD 362,07–USD 844,82 |
| Reserva de materiales           | Supuesto; equipo existente no valorizado | CLP 0                   | USD 0,00              |
| Total de inversión de escenario | No es inversión histórica ni cotización  | CLP 360.000–CLP 840.000 | USD 362,07–USD 844,82 |

### Operación mensual

Servicios obligatorios en el escenario local: USD 0/mes; no significa costo total cero. Equipo, tiempo, consumibles, distribución, almacenamiento y servicios opcionales deben presupuestarse cuando se necesiten.

| Concepto                        | Base                               | CLP/mes    | USD/mes   |
| ------------------------------- | ---------------------------------- | ---------- | --------- |
| Mantenimiento de escenario      | 2 h/mes                            | CLP 30.000 | USD 30,17 |
| Servicios externos de escenario | Ver composición; no factura actual | CLP 0      | USD 0,00  |
| Costo mensual modelado          | Tiempo + servicios                 | CLP 30.000 | USD 30,17 |

No se suman aquí impuestos, comisiones, dominio, sobreconsumo, reposición extraordinaria o equipo no valorizado. Costos de nube por proyecto/organización y cuotas gratuitas deben revisarse para la cuenta concreta. Una cuota de nube no sustituye la arquitectura ni la validación necesarias para escalar.

### Justificación y retorno

No se monetiza la diversión o el aprendizaje como si fueran caja. Si se explora una venta, ingreso bruto = unidades × precio; faltan precio aceptado, demanda, comisiones, impuestos y soporte para calcular utilidad.

Jugabilidad completa, comprensión de Ink-Pulse, estabilidad de perfil y rendimiento por target; ventas/retención sólo se estiman tras datos de uso.

ROI sólo se calcula cuando beneficios y costos comparables estén medidos: (beneficio − costo) / costo, indicando período y si el tiempo valorizado realmente se convierte en ahorro. Un costo de caja bajo no acredita rentabilidad ni valor comercial de la idea.

### Sensibilidad y decisión

- Con R ±10 %, cambia en ±10 % el costo CLP de servicios fijados en USD; el gasto local CLP no cambia por ese supuesto.
- Separar creación puntual de mantenimiento recurrente y reserva física de consumo.
- Registrar facturas/uso, horas observadas y aceptación antes de reemplazar estos escenarios por un presupuesto comprometido.
- Las monedas ficticias de un videojuego, saldos de clientes e inventario de una empresa no son ingresos del software.

### Referencias de servicios

[Supabase](https://supabase.com/pricing): Free y Pro desde USD 25/mes; [Vercel](https://vercel.com/pricing): Hobby y Pro desde USD 20/mes. Son referencias consultadas, no planes asignados al proyecto. [Gemini](https://ai.google.dev/gemini-api/docs/pricing) y [Firebase](https://firebase.google.com/pricing) requieren revisar producto/modelo, cuotas y condiciones antes de cuantificar consumo. El plan actual de Lovable u otro proveedor no se deduce de estas tarifas.

## Fuentes de implementación y contratos

[README.md](../README.md) · [Docs/README.md](README.md) · [Docs/SoftwareArchitecture.md](SoftwareArchitecture.md) · [Docs/CoreSystems.md](CoreSystems.md) · [Docs/GameplaySystems.md](GameplaySystems.md) · [Docs/Testing.md](Testing.md) · [Docs/AndroidBuild.md](AndroidBuild.md).
<!-- DOCUMENTACION:nivel-2026-10-29:END -->
