# 09 — Puesta en marcha y distribución

## Puesta en marcha y distribución por plataforma

**Disponibilidad:** Juego local con versión Windows y beta Android. La adaptación móvil continúa con ajustes de controles, interfaz, suspensión y rendimiento.

**Destinos:** Windows estable + Android beta; release final condicionada a validación.

Editor declarado en ProjectVersion.txt: `m_EditorVersion: 6000.3.11f1`. Los paquetes se resuelven desde Packages/manifest.json y packages-lock.json.

Abre la raíz del proyecto desde Unity Hub con la versión indicada del editor. Instala el módulo de la plataforma que vas a compilar y sigue la guía de escenas, pruebas y build. Conserva los archivos `.meta` al mover recursos.

### Secuencia de puesta en marcha

1. Preparar el runtime/editor y la configuración que indiquen las fuentes enlazadas; usar valores de prueba y restaurar el lockfile apropiado.
2. Ejecutar la modalidad local y sus comprobaciones existentes desde la carpeta indicada por esa guía. No confundir preview web con el runtime nativo o montaje físico.
3. Construir/instalar el artefacto de ese target y comprobar recorrido principal, error relevante y reapertura/actualización antes de declararlo entregado.

Ejecuta los comandos de comprobación antes de distribuir una compilación. Puedes consultar sus resultados remotos en la pestaña Actions del repositorio.

### Datos, servicios y distribución

Datos iniciales en Resources/PersistentDbSeeds y progreso en Application.persistentDataPath/db. JSON local para perfil, récords y catálogo; SQLite del servidor de feria es otro almacén limitado al host. No hay una base compartida de todos los jugadores ni sincronización integral de compras.

Para Windows, distribuye el ZIP completo, con el ejecutable y sus dependencias. Android utiliza un APK independiente; consulta su canal beta y los requisitos de instalación. Conserva la compatibilidad del perfil local al actualizar.

GitHub Releases distribuye los archivos de cada versión. Los servicios de servidor se despliegan por separado; consulta la guía de la plataforma para instalar o alojar la aplicación.

### Recuperación

Conservar el artefacto anterior y su procedencia. Retroceder código no revierte automáticamente datos o migraciones. Revisar compatibilidad de guardado/snapshot y restauración antes de instalar una versión antigua. Consulta las notas de la versión para conocer los requisitos y límites de la entrega.

## Código y documentación relacionada

[README.md](../README.md) · [Docs/README.md](README.md) · [Docs/SoftwareArchitecture.md](SoftwareArchitecture.md) · [Docs/CoreSystems.md](CoreSystems.md) · [Docs/GameplaySystems.md](GameplaySystems.md) · [Docs/Testing.md](Testing.md) · [Docs/AndroidBuild.md](AndroidBuild.md).
