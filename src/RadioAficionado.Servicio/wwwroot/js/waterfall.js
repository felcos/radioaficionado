'use strict';

/**
 * Waterfall Canvas 2D para visualizacion de espectro en tiempo real.
 * Recibe datos byte[] via SignalR y los renderiza con paleta de colores.
 */
class Waterfall {
    /**
     * @param {HTMLCanvasElement} canvas - Elemento canvas donde renderizar.
     * @param {HTMLElement} ejeX - Contenedor para etiquetas del eje X.
     */
    constructor(canvas, ejeX) {
        this.canvas = canvas;
        this.ctx = canvas.getContext('2d');
        this.ejeX = ejeX;

        this.frecuenciaTxHz = 1500;
        this.frecuenciaRxHz = 1500;
        this.frecuenciaMinHz = 0;
        this.rangoFrecuenciaHz = 5000;

        this.paleta = this._generarPaleta();
        this._ajustarTamano();

        // Observar cambios de tamano
        this._resizeObserver = new ResizeObserver(() => this._ajustarTamano());
        this._resizeObserver.observe(canvas.parentElement);

        // Click-to-tune
        this.canvas.addEventListener('click', (e) => this._alHacerClick(e));

        this.onFrecuenciaSeleccionada = null;
    }

    /**
     * Genera paleta de 256 colores: negro -> azul -> cyan -> amarillo -> rojo.
     * @returns {Uint8Array[]}
     */
    _generarPaleta() {
        const paleta = new Array(256);
        for (let i = 0; i < 256; i++) {
            let r, g, b;
            if (i < 64) {
                // Negro a azul oscuro
                r = 0;
                g = 0;
                b = Math.floor(i * 2);
            } else if (i < 128) {
                // Azul a cyan
                const t = (i - 64) / 64;
                r = 0;
                g = Math.floor(t * 255);
                b = 128 + Math.floor(t * 127);
            } else if (i < 192) {
                // Cyan a amarillo
                const t = (i - 128) / 64;
                r = Math.floor(t * 255);
                g = 255;
                b = Math.floor(255 * (1 - t));
            } else {
                // Amarillo a rojo
                const t = (i - 192) / 64;
                r = 255;
                g = Math.floor(255 * (1 - t));
                b = 0;
            }
            paleta[i] = [r, g, b];
        }
        return paleta;
    }

    /**
     * Ajusta el tamano del canvas al contenedor.
     */
    _ajustarTamano() {
        const rect = this.canvas.parentElement.getBoundingClientRect();
        const ancho = Math.floor(rect.width);
        const alto = Math.floor(rect.height) - 20; // Restar eje X

        if (ancho > 0 && alto > 0 && (this.canvas.width !== ancho || this.canvas.height !== alto)) {
            // Guardar imagen actual
            let imagenAnterior = null;
            if (this.canvas.width > 0 && this.canvas.height > 0) {
                imagenAnterior = this.ctx.getImageData(0, 0, this.canvas.width, this.canvas.height);
            }

            this.canvas.width = ancho;
            this.canvas.height = alto;

            // Restaurar imagen anterior si existia
            if (imagenAnterior) {
                this.ctx.putImageData(imagenAnterior, 0, 0);
            }

            this._dibujarEjeX();
        }
    }

    /**
     * Dibuja las etiquetas del eje X con frecuencias de audio.
     */
    _dibujarEjeX() {
        if (!this.ejeX) { return; }

        this.ejeX.innerHTML = '';
        const ancho = this.canvas.width;
        const pasoHz = 500;
        const numEtiquetas = Math.floor(this.rangoFrecuenciaHz / pasoHz);

        for (let i = 0; i <= numEtiquetas; i++) {
            const frecHz = this.frecuenciaMinHz + (i * pasoHz);
            const x = (i * pasoHz / this.rangoFrecuenciaHz) * ancho;

            const etiqueta = document.createElement('span');
            etiqueta.textContent = frecHz >= 1000 ? (frecHz / 1000).toFixed(1) + 'k' : frecHz.toString();
            etiqueta.style.position = 'absolute';
            etiqueta.style.left = x + 'px';
            etiqueta.style.fontSize = '0.55rem';
            this.ejeX.appendChild(etiqueta);
        }
    }

    /**
     * Agrega una nueva linea de espectro al waterfall.
     * Scroll vertical: nuevas lineas arriba, historial baja.
     * @param {Uint8Array} magnitudes - Magnitudes en bytes (0-255).
     * @param {number} resolucionHz - Resolucion de frecuencia en Hz.
     * @param {number} frecuenciaMinHz - Frecuencia minima en Hz.
     */
    agregarLinea(magnitudes, resolucionHz, frecuenciaMinHz) {
        const ancho = this.canvas.width;
        const alto = this.canvas.height;

        if (ancho <= 0 || alto <= 0) { return; }

        this.frecuenciaMinHz = frecuenciaMinHz;
        this.rangoFrecuenciaHz = magnitudes.length * resolucionHz;

        // Scroll: mover todo 1 pixel hacia abajo
        const imagenActual = this.ctx.getImageData(0, 0, ancho, alto - 1);
        this.ctx.putImageData(imagenActual, 0, 1);

        // Dibujar nueva linea en la fila superior
        const lineaImagen = this.ctx.createImageData(ancho, 1);
        const datos = lineaImagen.data;

        for (let x = 0; x < ancho; x++) {
            // Mapear pixel X a indice de magnitud
            const indice = Math.floor((x / ancho) * magnitudes.length);
            const valor = indice < magnitudes.length ? magnitudes[indice] : 0;

            const color = this.paleta[valor];
            const offset = x * 4;
            datos[offset] = color[0];     // R
            datos[offset + 1] = color[1]; // G
            datos[offset + 2] = color[2]; // B
            datos[offset + 3] = 255;      // A
        }

        this.ctx.putImageData(lineaImagen, 0, 0);

        // Dibujar cursores TX/RX
        this._dibujarCursores();
    }

    /**
     * Dibuja los cursores de frecuencia TX (rojo) y RX (verde).
     */
    _dibujarCursores() {
        const ancho = this.canvas.width;
        const alto = this.canvas.height;

        if (this.rangoFrecuenciaHz <= 0) { return; }

        // Cursor TX (rojo)
        const xTx = ((this.frecuenciaTxHz - this.frecuenciaMinHz) / this.rangoFrecuenciaHz) * ancho;
        if (xTx >= 0 && xTx < ancho) {
            this.ctx.strokeStyle = '#ff4444';
            this.ctx.lineWidth = 1;
            this.ctx.setLineDash([4, 4]);
            this.ctx.beginPath();
            this.ctx.moveTo(xTx, 0);
            this.ctx.lineTo(xTx, Math.min(30, alto));
            this.ctx.stroke();
            this.ctx.setLineDash([]);
        }

        // Cursor RX (verde)
        const xRx = ((this.frecuenciaRxHz - this.frecuenciaMinHz) / this.rangoFrecuenciaHz) * ancho;
        if (xRx >= 0 && xRx < ancho) {
            this.ctx.strokeStyle = '#00ff41';
            this.ctx.lineWidth = 1;
            this.ctx.setLineDash([4, 4]);
            this.ctx.beginPath();
            this.ctx.moveTo(xRx, 0);
            this.ctx.lineTo(xRx, Math.min(30, alto));
            this.ctx.stroke();
            this.ctx.setLineDash([]);
        }
    }

    /**
     * Maneja click en el waterfall para seleccionar frecuencia.
     * @param {MouseEvent} e
     */
    _alHacerClick(e) {
        const rect = this.canvas.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const frecuencia = (x / this.canvas.width) * this.rangoFrecuenciaHz + this.frecuenciaMinHz;
        const frecuenciaRedondeada = Math.round(frecuencia);

        this.frecuenciaRxHz = frecuenciaRedondeada;

        if (this.onFrecuenciaSeleccionada) {
            this.onFrecuenciaSeleccionada(frecuenciaRedondeada);
        }
    }

    /**
     * Establece la frecuencia TX.
     * @param {number} frecuenciaHz
     */
    establecerFrecuenciaTx(frecuenciaHz) {
        this.frecuenciaTxHz = frecuenciaHz;
    }

    /**
     * Establece la frecuencia RX.
     * @param {number} frecuenciaHz
     */
    establecerFrecuenciaRx(frecuenciaHz) {
        this.frecuenciaRxHz = frecuenciaHz;
    }

    /**
     * Libera recursos.
     */
    destruir() {
        if (this._resizeObserver) {
            this._resizeObserver.disconnect();
        }
    }
}
