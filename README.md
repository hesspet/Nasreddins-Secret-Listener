# Nasreddins-Secret-Listener

A magic device based on an ESP32 and a magnetometer to detect hidden magnets.
Ein magisches Gerät das als Herzstück einen ESP32 mit einem über I2C angebundnene Magnetometer auf einem Breakout-Board. 

## GY-273 QMC5883L Triple Axis Compass Magnetometer Sensor HMC5883L

Das Herzstück der Lösung ein Mangnetometer auf einem recht kleinen Breakoutboard. Quelle z.B.: https://www.amazon.de/TECNOIOT-GY-273-QMC5883L-Magnetometer-Compatible/dp/B07HMK1QNH. Diese kleinen Breakoutboards sind recht preisgünstig, so ca. 5-6 Euro. Warum genau dieser Chip. Die einfachste Erklärung ist, dass es dazu von Adafruid eine schöne Bibliothek gibt die den ganze I2C Overhead verwaltet. https://github.com/adafruit/Adafruit_HMC5883_Unified.

# Das geht doch gar nicht...

So oder ähnlich habe ich es an verschiedenen Stellen im Netz gelesen. Man kann mit dem Chip nicht einfach ein Magnet in der nähe erkennen. Naja - Unsinn. Es geht doch und sogar, so wie ich finde, recht gut.

# Roadmap

* Anbindung mit Bluetooth
* Umstellung auf Seeed Studio XIAO ESP32S3, da dieses ESP32 einen Anschluß und eine Ladeelektronik für kleine Lipo Akkus bereitstellt. Quelle: https://www.reichelt.de/de/de/shop/produkt/xiao_esp32s3_dual-core_wifi_bt5_0_ohne_header-358354.
* Erstellung der Smartphone App als Progresive Web Anwendung, so dass sowohl die iOs als auch die Android Welt unterstützt werden kann. (Aktuell noch unklar ob das mit BLE überhaupt geht) Ansonsten dann als MAUI lösung.
* Erweterung der Idee, so das das System auch vom Smartphone aus konfiguriert werden kann. Hier wäre vor allem das Thema Anpassung auf den Magneten ein Thema.
* Erarbeiten von Kunststückideen
  * Which Hand (die Mutter aller Magnettricks)
  * Nassreddin zaubert
  * Andere Anwendungsideen
 
  
