# IntroToRabbitMQ

Un sistema composto da più software che comunicano tra loro tramite messaggi. In particolare lo scambio di messaggi avviene attraverso un **Message Broker**. Un message broker è : . 
In particolare all'interno del progetto che sto seguendo è stato scelto di utilizzare RabbitMQ. RabbitMQ è un messagging broker open source che supporta diversi protocolli e offre diverse features. 
Il protocollo di base è ampq che prevede di base 3 entità: 

- Un publisher cioè l'applicazione che produce il messaggio
- Un Message Broker che prende il messaggio e lo instrada verso un consumer 
- un Consumer un'applicazione che consuma il messagfgio.

Nello specifico i messaggi sono pubblicati verso delle entità di ampq dette exchanges che si occupano di distribuire, secondo determinate regole dette *bindigns* i messaggi ricevuti ad altre entità di ampq ovvero le code. quindi il broker può consegnare il messaggio al consumer sottoscritto alla coda o il consumer può richiedere il messaggio dalla coda. 
L'algoritmo di routing con il quale un exchange instrada i messaggi verso le code dipende dal tipo di exchange e in ampq ci sono 4 tipi di questi più quello di default e presentano le seguenti caratteristiche:

- Default: Ha la peculiarità che automaticamente ogni nuova coda creata è collegata ad esso con una routing key uguale al nome della coda.
- Direct: Ideale per l'instradamento unicast. La consegna dei messaggi è basata su una routing key.
- fanOut: Ideale per l'instradamento broadcast in quanto inoltra i messaggi a tutte le code ad esso bindate ignorando la routing key.
- Topic: Ideale per l'instradamento multicast. La consegna dei messaggi è basata su una routing key e un wildcard pattern utilizzato per bindare le code all'exchange. 
- Headers: In questo caso lo scambio non è basato sulla routing key ma in base ad attributi espressi come message header.

 L'installazione di rabbit richiede l'installazione di diverse dipendenze come ad esempio ERLANG. Possiamo evitare ciò andando ad utilizzare un container di rabbit. 
 Andiamo quindi ad instanziare il nostro container con docker. Possiamo farlo da terminale oppure utilizzando kitematic. 

```
docker run -d --hostname my-rabbit --name rabbit1 rabbitmq:3-management
```
come possiamo notare l'immagine è quella di management che ci consentirà di avere un'interfaccia web, raggiungibile all'indirizzo dove possiamo monitorare le diverse entity di rabbit.
![#](/image#.png)

Per mostrare i concetti esposti in precedenza ho utilizzato un esempio semplice presente sul sito di rabbit. Ho creato una soluzione con due progetti. Il primo farà le veci del produttore del messaggio l'altro invece lo andrà a consumare. Analizziamo il *Sender* 