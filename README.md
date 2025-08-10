# Nasreddins Secret Listener

## Magie trifft Technik

Die Grundidee dieses kleinen Projekts war, eine bezahlbare technische Lösung zu finden, um den sogenannten „Which Hand“-Trick elektronisch umzusetzen.  
Als Amateurzauberer ist es wirtschaftlich nicht darstellbar, ein Gerät für über 200 € zu kaufen, um ein Kunststück von vielleicht ein oder zwei Minuten Dauer zu haben.  
Der DIY-Gedanke war naheliegend, und ich habe lange nach Beschreibungen zu diesem Trick gesucht.

Es gibt ganz verschiedene Ansätze, wie man das Kunststück ausführen kann – rein mental, mechanisch oder elektronisch.  
Viele der kommerziellen elektronischen Lösungen sind teuer oder so kompakt gebaut, dass sie für meine Zwecke nicht geeignet sind.  
Mein Ziel war es daher, eine robuste, nachvollziehbare und preisgünstige Variante mit Standardbauteilen zu entwickeln.

**Zielsetzung**
- Preisgünstig – möglichst unter 50 €
- Bauteile leicht zu beschaffen
- Bis auf wenige Lötarbeiten mit gängiger Hardware umsetzbar

## Technische Basis

Das Herzstück ist ein **QMC5883L-Magnetometer** auf einem kleinen Breakout-Board (GY-273).  
Dieser Sensor erkennt Magnete zuverlässig – ideal für den geplanten Einsatz.  
Es gibt von Adafruit eine gut dokumentierte Bibliothek, die den gesamten I²C-Overhead verwaltet und so die Ansteuerung erleichtert.

> Hinweis: Entgegen mancher Aussagen in Foren funktioniert die Erkennung eines Magneten in der Nähe mit diesem Chip durchaus zuverlässig.

## Roadmap

- Anbindung per Bluetooth
- Umstellung auf Seeed Studio XIAO ESP32S3 mit integriertem Akkuanschluss und Ladeschaltung  
  Quelle: <https://wiki.seeedstudio.com/xiao_esp32s3_getting_started/>
- Entwicklung einer Smartphone-App als Progressive Web App (oder alternativ als MAUI-App)
- Möglichkeit zur Magnet-Kalibrierung direkt vom Smartphone aus
- Sammlung von Kunststückideen, z. B.:
  - „Which Hand“ – der Klassiker
  - „Nasreddin zaubert“
  - Weitere Anwendungen

## Wer oder was ist Nassreddin?

Nasreddin Hodscha (auch bekannt als Nasreddin, Nasreddin Hoca oder Nasreddin Hodja) ist eine legendäre Figur aus dem türkischen, persischen und arabischen Kulturraum.  
Er gilt als Volksweisheitslehrer, Geschichtenerzähler und Schelm, der mit Humor und oft überraschenden Pointen Alltagsweisheiten vermittelt.  
Die Anekdoten um Nasreddin reichen bis ins 13. Jahrhundert zurück und sind in vielen Ländern verbreitet.  
Mehr dazu findet man zum Beispiel hier:  
- <https://de.wikipedia.org/wiki/Nasreddin>  

## Persönliche Anekdote

Als der Trick **Mr. Gloves** von Juan Pablo herauskam, habe ich ihn sofort gekauft.  
Die Figur trägt dabei eine markante Mütze – diese hat dazu geführt, dass wir ihr spontan den Namen **Nasr ad-Din al-Quffaz (نصر الدين القفاز)** gaben, was übersetzt etwa „Nasreddin der Handschuh“ bedeutet.  
So entstand auch der Name dieses Projekts. Und glaubt mir, er hat viel von 

