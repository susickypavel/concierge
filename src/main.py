import hikari
import os
from dotenv import load_dotenv

load_dotenv()

token = os.environ.get("DISCORD_BOT_TOKEN")

bot = hikari.GatewayBot(token=token)


@bot.listen()
async def ping(event: hikari.GuildMessageCreateEvent) -> None:
    if not event.is_human:
        return

    me = bot.get_me()

    if me.id in event.message.user_mentions_ids:
        await event.message.respond("Pong!")

bot.run()
