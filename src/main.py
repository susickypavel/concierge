import os
import lightbulb

from dotenv import load_dotenv
from hikari import Intents

load_dotenv()

token = os.environ.get("DISCORD_BOT_TOKEN")

bot_intents = (
        Intents.GUILD_MESSAGES |
        Intents.MESSAGE_CONTENT
)

bot = lightbulb.BotApp(token=token, prefix="!", intents=bot_intents)


@bot.command
@lightbulb.command("ping", "checks the bot is alive")
@lightbulb.implements(lightbulb.PrefixCommand)
async def ping(ctx: lightbulb.Context) -> None:
    await ctx.respond("Pong!")


bot.run()
