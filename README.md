# Nasreddins-Secret-Listener

## Die Anwendung

Die Grundidee dieses kleinen Gimmicks war eine bezahlbare Technische Lösung zu finden um den sogenannten "Which Hand" Trick elektronisch umzusetzen. Also Amateurzauberer ist es wirschaftlich nicht Darstellbar ein Gerät für über 200€ zu kaufen um einen Kunststück von einer oder zwei Minuten zu haben. Und klar, die der DIY Virus hat mal wieder zugeschlagen. Ich habe lange nach Beschreibungen zu dem Which Hand trick gesucht.

Es gibt ganz verschiedene Ansätze wie man das Kunstück ausführen kann.

* Rein Mental (z.B. wie in dem Kunststück Overlooked)
* Mit Zinken
* Mit einem mechanischen Detector (Which Hand by Christopher Taylor, eine absolut geniale Lösung)
* Elektronisch we z.B. Sixt Sence von Hugo Shelly (Was lt. meinen Infos nicht mehr neu hergestellt wird, Webseite: https://www.hugoshelley.com ist down.)

Die Motivation mir selbst so ein kleines "Which Hand" Tool zu basteln kommt vor allem auch aus der Tatsache, dass ich etwas Sehbehindert bin und so ein System vo das von Christoper Talor einfach nicht nutzen kann. Es ist einfach zu klein.

Nun aber die harten Anforderungen

* Preisgünstig, möglichst un 50 Euro zu realisieren
* Highttech, aber außer ein paar Lötpunkte soll alles mit gut erhältlichen Komponenten realisierbar sein, auch wenn sich daraus ggf. Einschränkungen in der Benutzbarkeit ergeben.

Also mal zum Thema Magnetometer geforscht. Die Frage nach Magnetometer zum erkennen von Magneten taucht im Internet immer mal wieder auf, aber so richtig Erhellendes habe ich da nicht gefunen.

DIY Prozess: Ich besorge mir ein Breakout Board mit einem Magnetometer Chip (QMC5883L) und hänge den mit I2C an einen Microkontroller den ich eh schon hatte.

## GY-273 QMC5883L Triple Axis Compass Magnetometer Sensor HMC5883L

Das Herzstück der Lösung ist also ein Mangnetometer auf einem recht kleinen Breakoutboard. Quelle z.B.: https://www.amazon.de/TECNOIOT-GY-273-QMC5883L-Magnetometer-Compatible/dp/B07HMK1QNH. Diese kleinen Breakoutboards sind recht preisgünstig, so ca. 5-6 Euro. Warum genau dieser Chip. Die einfachste Erklärung ist, dass es dazu von Adafruid eine schöne Bibliothek gibt die den ganze I2C Overhead verwaltet. https://github.com/adafruit/Adafruit_HMC5883_Unified.

# Das geht doch gar nicht...

So oder ähnlich habe ich es an verschiedenen Stellen im Netz gelesen. Man kann mit dem Chip nicht einfach ein Magnet in der nähe erkennen. Naja - Unsinn. Es geht doch und sogar, so wie ich finde, recht gut.

# Roadmap

* Anbindung mit Bluetooth
* Umstellung auf Seeed Studio XIAO ESP32S3, da dieses ESP32 einen Anschluß und eine Ladeelektronik für kleine Lipo Akkus bereitstellt. Quelle: https://www.reichelt.de/de/de/shop/produkt/xiao_esp32s3_dual-core_wifi_bt5_0_ohne_header-358354. Wiki: https://wiki.seeedstudio.com/xiao_esp32s3_getting_started/ (Board bereits bestellt :-) )
* Erstellung der Smartphone App als Progresive Web Anwendung, so dass sowohl die iOs als auch die Android Welt unterstützt werden kann. (Aktuell noch unklar ob das mit BLE überhaupt geht) Ansonsten dann als MAUI lösung.
* Erweterung der Idee, so das das System auch vom Smartphone aus konfiguriert werden kann. Hier wäre vor allem das Thema Anpassung auf den Magneten ein Thema.
* Erarbeiten von Kunststückideen
  * Which Hand (die Mutter aller Magnettricks)
  * Nassreddin zaubert
  * Andere Anwendungsideen
 
  
