# Gestire la comunicazione tra applicazioni mediante il broker di messaggi RabbitMQ. 

Il progetto su cui sto lavorando è basato su di un'architettura composta da più applicazioni che comunicano tra loro tramite scambio di messaggi. In genere, questa comunicazione avviene seguendo il pattern _Publish/Subscribe_.
Questo pattern prevede che un'applicazione possa comunicare in maniera _asincrona_ con più entità interessate senza che ci sia accoppiamento tra le parti. Le entità coinvolte sono le seguenti:

![1](/image1.png)

- **Publisher** - L'applicazione che produce il messaggio
- **Message Broker** - Il _MOM_(Message Oriented Middleware) che prende il messaggio e lo instrada verso un consumer 
- **Consumer** - L'applicazione che consuma il messaggio.

Per il progetto viene utilizzato **[RabbitMQ](https://www.rabbitmq.com/)**, un messagging broker open source che supporta diversi protocolli e che offre diverse features.

![2](/image2.gif)
 
Di base RabbitMQ implementa il protocolo **_AMQP.0-9-1_**(Advanced Message Queuing Protocol) il quale prevede che i messaggi siano pubblicati verso delle entità di AMQP dette **_Exchanges_** il cui ruolo è quello di distribuire, secondo determinate regole dette **_bindigns_**, i messaggi ricevuti ad altre entità del protocollo ovvero le **_Queue_** a cui può sottoscriversi uno o più consumers.

![4](/image3.png)

L'algoritmo di routing con il quale possono essere instradati i messaggi verso le code dipende dal tipo di exchange e in AMQP ne sono definiti quattro oltre quello di _default_ e presentano le seguenti caratteristiche:

- **Default**: Ha la peculiarità che ogni nuova coda creata è collegata automaticamente ad esso con una routing key uguale al nome della coda.
- **Direct**: Ideale per l'instradamento unicast. La consegna dei messaggi è basata su una routing key.
- **FanOut**: Ideale per l'instradamento broadcast in quanto inoltra i messaggi a tutte le code ad esso bindate ignorando la routing key.
- **Topic**: Ideale per l'instradamento multicast. La consegna dei messaggi è basata su una routing key e un _wildcard_ pattern utilizzato per bindare le code all'exchange. 
- **Headers**: In questo caso lo scambio non è basato sulla routing key ma in base ad attributi espressi come message header.

Sia le code che gli exchange hanno proprietà per le quali possono sopravvivere al riavvio del broker e possono autocancellarsi rispettivamente in mancanza di consumer o di code associate. Le code inoltre possono essere esclusive cioè legate ad una sola connessione.

Mettiamo in pratica in concetti espressi finora. Invece di installare RabbitMQ in locale ho utilizzato un _container_ Docker sia per una questione di praticità dovuta al fatto che l'immagine dockerizzata è già pronta all'uso sia per dare l'idea di utilizzare un broker istanziato su un'ambiente separato rispetto al publisher ed al subscriber che verranno definiti in seguito.
Per la creazione lanciamo il comando seguente con cui specifichiamo l'hostname, il nome del container e l'immagine di RabbitMQ che vogliamo istanziare. 

```
docker run -d --hostname my-rabbit --name rabbit1 -p "5672:5672" -p "15672:15672" rabbitmq:3-management
```
Ho mappato,inoltre, la porta di default e quella della Web UI di management tra container e localhost per potervi accedere senza problemi.

Ho scelto l'immagine di management proprio per la presenza di un'interfaccia web, raggiungibile all'indirizzo `http://localhost:15672/`, con la quale è possibile interagire e monitorare le diverse entities del broker, un alternativa più intuitiva rispetto alla CLI `rabbitmqctl`. 

![5](/image5.png)

Utilizzando degli esempi messi a disposizione sul sito di RabbitMQ ho creato due _console application_ in _.NET Core_. Una farà da Publisher e l'altra da Subscriber. Come per altri linguaggi di programmazione RabbitMQ fornisce un client per il linguaggio .NET installabile mediante il gestore di pacchetti _Nuget_. 
Analizziamo il produttore del messaggio che ho definito come *Sender*.

```cs{.line-numbers}
class Sender
{
    static void Main(string[] args)
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: "QueueDemo",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            string message = "Demo Message";
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "",
                                 routingKey: "QueueDemo",
                                 basicProperties: null,
                                 body: body);
            Console.WriteLine("Sent {0}", message);
        }

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }
}
```

Creiamo una connessione verso l'EndPoint, nel nostro caso, _localhost_ istanziando una _ConnectionFactory_ . Eseguendo il programma in debug possiamo vedere che verso l'endpoint viene creata una connessione con protocollo AMQP con la porta di default di RabbitMQ.

 ![6](/image6.png)

 Dichiariamo quindi una coda definendo la proprietà _Name_ (_QueueDemo_). Creiamo il messaggio da inviare (una semplice stringa) e nel metodo _BasicPublish_ indichiamo l'exchange di default (con la stringa vuota), la _routing key_ (uguale al nome della coda) e inseriamo il messaggio da pubblicare nel _body_.
Lanciamo l'applicazione Sender :

 ![7](/image7.png)

Nella schermata di _Overview_ della WebUI notiamo che il numero delle code è aumentato, infatti è stata creata la coda _QueueDemo_ è c'è un messaggio in coda.

 ![8](/image8.png) 

  Nei dettagli specifici di _QueueDemo_ possiamo vedere l'exchange a cui è collegata 

 ![9](/image9.png)

 e cliccando sul _button_ **Get Message(s)** ci viene mostrato il _Payload_ del nostro messeggio che corrisponde alla stringa creata nel Sender.

 ![10](/image10.png)
 
 Il messaggio, quindi, è correttamente accodato ed è in attesa di essere consumato.
Analizziamo l'applicazione che farà da Subscriber che ho chiamato _Receiver_. 
Allo stesso modo del _Sender_ creiamo una connessione e dichiariamo la coda _DemoQueue_. Creiamo il consumer vero e proprio mediante la classe _EventBasicConsumer_. Col metodo _Received_ andiamo a definire l'evento che verrà scatenato alla ricezione del messaggio assegnando le proprietà del _BasicDeliverEventArgs_ **ea** che contiene le tutte le proprietà relative al messaggio consegnato dal broker. Infine col metodo _BasicConsume_ avviamo il _consumer_ appena definito.

```cs{.line-numbers}
class Receiver
{
    static void Main(string[] args)
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using (var connection = factory.CreateConnection())
        {
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "QueueDemo",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine("Received {0}", message);
                };
                channel.BasicConsume(queue: "QueueDemo",
                                     autoAck: true,
                                     consumer: consumer);
                Console.WriteLine("Press [enter] to exit.");
                Console.ReadLine();
            }
        }
    }
}
```
Eseguendo l'applicazione il messaggio viene consumato correttamente. 

 ![11](/image11.jpg)

 ![12](/image12.png)

 Se avessimo più messaggi in coda e avessimo più consumer in attesa il _load balancing_ è gestito di default con uno scheduling _Round Robin_ per cui nessun consumer avrà priorità rispetto agli altri.

Per essere sicuri che il messaggio sia stato conseganto al consumer possiamo modificare la modalità di _acknowledge_ del messaggio. All'interno del metodo _Received_ del consumer invochiamo il metodo BasicAck con il quale viene fatto l'acknowledge del messaggio. Tra gli argomenti di questo metodo c'è il _Delivery Tag_ che identifica univocamente la consegna.
Bisogna inoltre settare nel _BasicConsume_ l'autoAck a false.

```cs{.line-numbers}
channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
```


 Sulla WebUI possiamo vedere come il messaggio sia stato consumato correttamente e sia stato inviato un ack da parte del consumer.

 ![13](/image13.png)

RabbitMQ può essere un'ottima scelta per la comunicazione tra applicazioni, microservizi o comunque tutte quelle soluzioni software per cui sia necessario utilizzare un'architettura distribuita. Come middleware open source offre moltissimi vantaggi tra cui il disaccopiamento tra le varie entità per cui è possibile farne il deploy separatamente oppure la comunicazione asincrona che consente alle applicazioni di proseguire il flusso di esecuzione senza interruzioni.
A differenza ad esempio della comunicazione HTTP mediante REST API offre una maggiore affidabilità nello scambio di informazioni tra applicazioni grazie sia a meccanismi di acknowledge e riconsegna dei messaggi sia a meccanismi di robustezza e ridondanza(cluster di nodi). Naturalmente in questo articolo ho solo mostrato le caratteristiche di base di RabbitMQ che per me sono state molto utili per iniziare ad interagire con questa modalità di comunicazione. 
Spero di poter raccontare qualche aspetto avanzato al più presto in un prossimo articolo. Intanto lascio il link al repository GitHub creato per l'occasione https://github.com/intersect88/IntroToRabbitMQ. 
Happy Coding!

