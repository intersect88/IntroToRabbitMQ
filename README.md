# IntroToRabbitMQ

Un sistema composto da più software che comunicano tra loro tramite messaggi. In particolare lo scambio di messaggi avviene attraverso un **Message Broker**. Un message broker è : . 
In particolare all'interno del progetto che sto seguendo è stato scelto di utilizzare RabbitMQ. RabbitMQ è un messagging broker open source che supporta diversi protocolli e offre diverse features. 
Il protocollo di base è ampq che prevede di base 3 entità: 
    - Un publisher cioè l'applicazione che produce il messaggio
    - Un Message Broker che prende il messaggio e lo instrada verso un consumer 
    - un Consumer un'applicazione che consuma il messagfgio.

Nello specifico i messaggi sono pubblicati verso delle entità di ampq dette exchanges che si occupano di distribuire, secondo determinate regole dette *bindigns* i messaggi ricevuti ad altre entità di ampq ovvero le code. quindi il broker può consegnare il messaggio al consumer sottoscritto alla coda o il consumer può richiedere il messaggio dalla coda. 
L'algoritmo di routing con il quale un exchange instrada i messaggi verso le code dipende dal tipo di exchange e in ampq ci sono 4 ipi di questi:

- Direct
- fanOut
- Topic
- Headers