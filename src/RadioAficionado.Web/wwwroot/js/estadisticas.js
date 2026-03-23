'use strict';

/**
 * Modulo de graficos para el dashboard de estadisticas.
 * Utiliza Chart.js 4.x para renderizar graficos de barras, pastel, lineas y dona.
 */

const COLORES_BANDAS = [
    '#58a6ff', '#3fb950', '#d29922', '#f85149',
    '#bc8cff', '#79c0ff', '#56d364', '#e3b341',
    '#ff7b72', '#d2a8ff', '#a5d6ff', '#7ee787'
];

const COLORES_MODOS = [
    '#58a6ff', '#3fb950', '#d29922', '#f85149',
    '#bc8cff', '#79c0ff', '#56d364', '#e3b341',
    '#ff7b72', '#d2a8ff', '#a5d6ff', '#7ee787',
    '#ffa657', '#ff9bce', '#9ecbff', '#b1bac4'
];

const COLORES_CONTINENTES = [
    '#58a6ff', '#3fb950', '#d29922', '#f85149',
    '#bc8cff', '#79c0ff', '#56d364'
];

const OPCIONES_BASE = {
    responsive: true,
    maintainAspectRatio: true,
    plugins: {
        legend: {
            labels: {
                color: '#c9d1d9',
                font: { size: 12 }
            }
        }
    }
};

/**
 * Carga datos JSON desde un endpoint y retorna el objeto parseado.
 * @param {string} url - URL del endpoint JSON.
 * @returns {Promise<object>} Datos parseados.
 */
async function cargarDatos(url) {
    const respuesta = await fetch(url);
    if (!respuesta.ok) {
        throw new Error(`Error al cargar datos: ${respuesta.status}`);
    }
    return respuesta.json();
}

/**
 * Crea el grafico de barras con QSOs por banda.
 */
async function crearGraficoBandas() {
    const canvas = document.getElementById('graficoBandas');
    if (!canvas) return;

    try {
        const datos = await cargarDatos('/Estadisticas/DatosBandas');

        new Chart(canvas, {
            type: 'bar',
            data: {
                labels: datos.etiquetas,
                datasets: [{
                    label: 'QSOs',
                    data: datos.cantidades,
                    backgroundColor: COLORES_BANDAS.slice(0, datos.etiquetas.length),
                    borderColor: COLORES_BANDAS.slice(0, datos.etiquetas.length),
                    borderWidth: 1,
                    borderRadius: 4
                }]
            },
            options: {
                ...OPCIONES_BASE,
                plugins: {
                    ...OPCIONES_BASE.plugins,
                    legend: { display: false }
                },
                scales: {
                    x: {
                        ticks: { color: '#8b949e' },
                        grid: { color: '#30363d' }
                    },
                    y: {
                        beginAtZero: true,
                        ticks: {
                            color: '#8b949e',
                            stepSize: 1,
                            precision: 0
                        },
                        grid: { color: '#30363d' }
                    }
                }
            }
        });
    } catch (error) {
        console.error('Error al crear grafico de bandas:', error);
    }
}

/**
 * Crea el grafico de pastel/dona con QSOs por modo.
 */
async function crearGraficoModos() {
    const canvas = document.getElementById('graficoModos');
    if (!canvas) return;

    try {
        const datos = await cargarDatos('/Estadisticas/DatosModos');

        new Chart(canvas, {
            type: 'doughnut',
            data: {
                labels: datos.etiquetas,
                datasets: [{
                    data: datos.cantidades,
                    backgroundColor: COLORES_MODOS.slice(0, datos.etiquetas.length),
                    borderColor: '#1c2128',
                    borderWidth: 2
                }]
            },
            options: {
                ...OPCIONES_BASE,
                plugins: {
                    ...OPCIONES_BASE.plugins,
                    legend: {
                        position: 'bottom',
                        labels: {
                            color: '#c9d1d9',
                            font: { size: 11 },
                            padding: 10,
                            usePointStyle: true,
                            pointStyle: 'circle'
                        }
                    }
                }
            }
        });
    } catch (error) {
        console.error('Error al crear grafico de modos:', error);
    }
}

/**
 * Crea el grafico de lineas con QSOs por mes (ultimos 12 meses).
 */
async function crearGraficoTemporal() {
    const canvas = document.getElementById('graficoTemporal');
    if (!canvas) return;

    try {
        const datos = await cargarDatos('/Estadisticas/DatosTemporales');

        new Chart(canvas, {
            type: 'line',
            data: {
                labels: datos.etiquetas,
                datasets: [{
                    label: 'QSOs',
                    data: datos.cantidades,
                    borderColor: '#58a6ff',
                    backgroundColor: 'rgba(88, 166, 255, 0.1)',
                    borderWidth: 2,
                    fill: true,
                    tension: 0.3,
                    pointBackgroundColor: '#58a6ff',
                    pointBorderColor: '#58a6ff',
                    pointRadius: 4,
                    pointHoverRadius: 6
                }]
            },
            options: {
                ...OPCIONES_BASE,
                plugins: {
                    ...OPCIONES_BASE.plugins,
                    legend: { display: false }
                },
                scales: {
                    x: {
                        ticks: {
                            color: '#8b949e',
                            maxRotation: 45
                        },
                        grid: { color: '#30363d' }
                    },
                    y: {
                        beginAtZero: true,
                        ticks: {
                            color: '#8b949e',
                            stepSize: 1,
                            precision: 0
                        },
                        grid: { color: '#30363d' }
                    }
                }
            }
        });
    } catch (error) {
        console.error('Error al crear grafico temporal:', error);
    }
}

/**
 * Crea el grafico de dona con QSOs por continente.
 */
async function crearGraficoContinentes() {
    const canvas = document.getElementById('graficoContinentes');
    if (!canvas) return;

    try {
        const datos = await cargarDatos('/Estadisticas/DatosContinentes');

        new Chart(canvas, {
            type: 'doughnut',
            data: {
                labels: datos.etiquetas,
                datasets: [{
                    data: datos.cantidades,
                    backgroundColor: COLORES_CONTINENTES.slice(0, datos.etiquetas.length),
                    borderColor: '#1c2128',
                    borderWidth: 2
                }]
            },
            options: {
                ...OPCIONES_BASE,
                plugins: {
                    ...OPCIONES_BASE.plugins,
                    legend: {
                        position: 'bottom',
                        labels: {
                            color: '#c9d1d9',
                            font: { size: 11 },
                            padding: 10,
                            usePointStyle: true,
                            pointStyle: 'circle'
                        }
                    }
                }
            }
        });
    } catch (error) {
        console.error('Error al crear grafico de continentes:', error);
    }
}

// Inicializar todos los graficos al cargar la pagina
document.addEventListener('DOMContentLoaded', function () {
    crearGraficoBandas();
    crearGraficoModos();
    crearGraficoTemporal();
    crearGraficoContinentes();
});
